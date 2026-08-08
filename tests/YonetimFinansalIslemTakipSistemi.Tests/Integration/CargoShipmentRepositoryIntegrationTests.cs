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
///
/// İZOLASYON: her test kendi kayıtlarını <see cref="TestMarker"/> ile
/// işaretler, sonunda siler ve sayaçları eski değerine döndürür. Silmenin
/// işe yaradığı DOĞRULANIR (bkz. LiveDatabaseFixture.AssertNoResidueAsync) —
/// artık satır bırakan bir temizlik, hatayı bir sonraki koşuya taşır.
/// </summary>
[Collection(LiveDatabaseCollection.Name)] // diğer canlı-DB sınıfıyla seri çalışır (flaky önlenir)
public class CargoShipmentRepositoryIntegrationTests(LiveDatabaseFixture db)
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

    private static async Task CleanupAsync(AppDbContext ctx, Dictionary<int, long> counters)
    {
        await LiveDatabaseFixture.DeleteShipmentsAsync(ctx, TestMarker);
        await LiveDatabaseFixture.RestoreCountersAsync(ctx, counters);
    }

    [Fact]
    public async Task Create_ArdisikNumaralarUretir_VeSayaciArtirir()
    {
        if (!db.IsAvailable) return; // DB erişilemiyor — test atlanır

        await using var ctx = db.CreateContext();
        var counters = await LiveDatabaseFixture.SnapshotCountersAsync(ctx);
        try
        {
            var repo = new CargoShipmentRepository(db.Factory!, new NoOpSystemLogService());
            var baseSeq = counters.GetValueOrDefault((int)CargoShipmentDirection.Outgoing);

            var first  = NewShipment(CargoShipmentDirection.Outgoing);
            var second = NewShipment(CargoShipmentDirection.Outgoing);
            await repo.AddWithAutoNumberAsync(first);
            await repo.AddWithAutoNumberAsync(second);

            Assert.Equal(CargoNumberFormatter.Format(CargoShipmentDirection.Outgoing, baseSeq + 1), first.ShipmentNumber);
            Assert.Equal(CargoNumberFormatter.Format(CargoShipmentDirection.Outgoing, baseSeq + 2), second.ShipmentNumber);

            var current = await LiveDatabaseFixture.SnapshotCountersAsync(ctx);
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
        if (!db.IsAvailable) return;

        await using var ctx = db.CreateContext();
        var counters = await LiveDatabaseFixture.SnapshotCountersAsync(ctx);
        try
        {
            var repo = new CargoShipmentRepository(db.Factory!, new NoOpSystemLogService());
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

            var current = await LiveDatabaseFixture.SnapshotCountersAsync(ctx);
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
        if (!db.IsAvailable) return;

        await using var ctx = db.CreateContext();
        var counters = await LiveDatabaseFixture.SnapshotCountersAsync(ctx);
        try
        {
            // Gerçek eşzamanlılık: iki repository aynı anda numara ister; her repo işlem başına
            // kendi context/connection'ını açar (Sprint 21) → sayaç satır kilidi ikisini serileştirmelidir
            var repoA = new CargoShipmentRepository(db.Factory!, new NoOpSystemLogService());
            var repoB = new CargoShipmentRepository(db.Factory!, new NoOpSystemLogService());

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
