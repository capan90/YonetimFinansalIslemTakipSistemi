using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

namespace YonetimFinansalIslemTakipSistemi.Tests.Integration;

/// <summary>
/// Gerçek dev PostgreSQL üzerinde repository SQL doğrulaması.
/// DB erişilemiyorsa testler işlem yapmadan geçer (offline/CI kırılmaz).
/// Testler kendi oluşturduğu kayıtları siler ve sayaçları eski değerine geri yükler.
/// </summary>
public class CargoShipmentRepositoryIntegrationTests
{
    private const string TestMarker = "__integration_test__";

    private static CargoShipment NewShipment(CargoShipmentDirection direction) => new()
    {
        Id           = Guid.NewGuid(),
        Direction    = direction,
        ShipmentDate = DateTime.UtcNow.Date,
        Status       = CargoShipmentStatus.Prepared,
        Notes        = TestMarker,
        CreatedAt    = DateTime.UtcNow,
        IsDeleted    = false
    };

    /// <summary>Sayaç değerlerini kaydeder; test sonunda kayıtları siler ve sayaçları geri yükler.</summary>
    private static async Task<Dictionary<int, long>> SnapshotCountersAsync(AppDbContext ctx)
        => await ctx.CargoNumberCounters.AsNoTracking()
            .ToDictionaryAsync(c => (int)c.Direction, c => c.LastValue);

    private static async Task CleanupAsync(AppDbContext ctx, Dictionary<int, long> counters)
    {
        await ctx.Database.ExecuteSqlAsync(
            $"DELETE FROM cargo_shipments WHERE \"Notes\" = {TestMarker}");
        foreach (var (direction, value) in counters)
            await ctx.Database.ExecuteSqlAsync($@"
                UPDATE cargo_number_counters SET ""LastValue"" = {value}
                WHERE ""Direction"" = {direction}");
    }

    [Fact]
    public async Task Create_ArdisikNumaralarUretir_VeSayaciArtirir()
    {
        await using var ctx = IntegrationDb.TryCreateContext()!;
        if (ctx is null) return; // DB erişilemiyor — test atlanır

        var counters = await SnapshotCountersAsync(ctx);
        try
        {
            var repo = new CargoShipmentRepository(IntegrationDb.TryCreateFactory()!, new NoOpSystemLogService());
            var baseSeq = counters.GetValueOrDefault((int)CargoShipmentDirection.Outgoing);

            var first  = NewShipment(CargoShipmentDirection.Outgoing);
            var second = NewShipment(CargoShipmentDirection.Outgoing);
            await repo.AddWithAutoNumberAsync(first);
            await repo.AddWithAutoNumberAsync(second);

            Assert.Equal(CargoNumberFormatter.Format(CargoShipmentDirection.Outgoing, baseSeq + 1), first.ShipmentNumber);
            Assert.Equal(CargoNumberFormatter.Format(CargoShipmentDirection.Outgoing, baseSeq + 2), second.ShipmentNumber);

            var current = await SnapshotCountersAsync(ctx);
            Assert.Equal(baseSeq + 2, current[(int)CargoShipmentDirection.Outgoing]);
        }
        finally
        {
            await CleanupAsync(ctx, counters);
        }
    }

    [Fact]
    public async Task SonNumaraSilinirse_GeriAlinirVeYenidenKullanilir_AradakiSilinmez()
    {
        await using var ctx = IntegrationDb.TryCreateContext()!;
        if (ctx is null) return;

        var counters = await SnapshotCountersAsync(ctx);
        try
        {
            var repo = new CargoShipmentRepository(IntegrationDb.TryCreateFactory()!, new NoOpSystemLogService());
            var baseSeq = counters.GetValueOrDefault((int)CargoShipmentDirection.Outgoing);

            var s1 = NewShipment(CargoShipmentDirection.Outgoing);
            var s2 = NewShipment(CargoShipmentDirection.Outgoing);
            var s3 = NewShipment(CargoShipmentDirection.Outgoing);
            await repo.AddWithAutoNumberAsync(s1);
            await repo.AddWithAutoNumberAsync(s2);
            await repo.AddWithAutoNumberAsync(s3);

            // Son numara silinir → sayaç geri alınır, numara serbest kalır
            s3.IsDeleted = true;
            s3.DeletedAt = DateTime.UtcNow;
            var reclaimed = await repo.SoftDeleteWithNumberReclaimAsync(s3);
            Assert.Equal(baseSeq + 3, reclaimed);
            Assert.Null(s3.ShipmentNumber);

            // Sonraki kayıt geri alınan numarayı yeniden kullanır
            var s4 = NewShipment(CargoShipmentDirection.Outgoing);
            await repo.AddWithAutoNumberAsync(s4);
            Assert.Equal(CargoNumberFormatter.Format(CargoShipmentDirection.Outgoing, baseSeq + 3), s4.ShipmentNumber);

            // Aradaki numara silinir → sayaç değişmez, numara korunur
            s1.IsDeleted = true;
            s1.DeletedAt = DateTime.UtcNow;
            var notReclaimed = await repo.SoftDeleteWithNumberReclaimAsync(s1);
            Assert.Null(notReclaimed);
            Assert.Equal(CargoNumberFormatter.Format(CargoShipmentDirection.Outgoing, baseSeq + 1), s1.ShipmentNumber);

            var current = await SnapshotCountersAsync(ctx);
            Assert.Equal(baseSeq + 3, current[(int)CargoShipmentDirection.Outgoing]);
        }
        finally
        {
            await CleanupAsync(ctx, counters);
        }
    }

    [Fact]
    public async Task EszamanliIkiKayit_DuplicateNumaraUretmez()
    {
        await using var ctx = IntegrationDb.TryCreateContext()!;
        if (ctx is null) return;

        var counters = await SnapshotCountersAsync(ctx);
        try
        {
            // Gerçek eşzamanlılık: iki repository aynı anda numara ister; her repo işlem başına
            // kendi context/connection'ını açar (Sprint 21) → sayaç satır kilidi ikisini serileştirmelidir
            var repoA = new CargoShipmentRepository(IntegrationDb.TryCreateFactory()!, new NoOpSystemLogService());
            var repoB = new CargoShipmentRepository(IntegrationDb.TryCreateFactory()!, new NoOpSystemLogService());

            var a = NewShipment(CargoShipmentDirection.Incoming);
            var b = NewShipment(CargoShipmentDirection.Incoming);
            await Task.WhenAll(repoA.AddWithAutoNumberAsync(a), repoB.AddWithAutoNumberAsync(b));

            Assert.NotNull(a.ShipmentNumber);
            Assert.NotNull(b.ShipmentNumber);
            Assert.NotEqual(a.ShipmentNumber, b.ShipmentNumber);
        }
        finally
        {
            await CleanupAsync(ctx, counters);
        }
    }
}
