using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests.Integration;

/// <summary>
/// TEST VERİSİ İZOLASYONUNUN KENDİSİNİ doğrular (Faz F2).
///
/// Integration testleri PAYLAŞILAN dev veritabanına yazıyor. Her test kendi
/// kayıtlarını temizliyor — ama temizliğin işe yaradığını şimdiye kadar
/// hiçbir şey kontrol etmiyordu. Temizlik sessizce eşleşmezse artık satırlar
/// birikir ve hata BU testte değil, SONRAKİNDE görünür: sayımlar tutmaz,
/// kaynağı da belli olmaz.
///
/// Aşağıdaki testler temizlik kapısının üç davranışını sabitler:
/// gerçekten siler, soft-delete edilmiş artığı da görür, artık kalırsa
/// SUSMAZ.
/// </summary>
[Collection(LiveDatabaseCollection.Name)]
public class LiveDatabaseIsolationTests(LiveDatabaseFixture db)
{
    private const string Marker = "__isolation_guard_test__";

    private static CargoShipment Marked(string notes) => new()
    {
        Id           = Guid.NewGuid(),
        Direction    = CargoShipmentDirection.Outgoing,
        ShipmentDate = DateTime.UtcNow.Date,
        Status       = CargoShipmentStatus.Prepared,
        Notes        = notes,
        CreatedAt    = DateTime.UtcNow,
        IsDeleted    = false
    };

    [Fact]
    public async Task Temizlik_isaretli_kayitlari_gercekten_siliyor()
    {
        if (!db.IsAvailable) return;

        await using var ctx = db.CreateContext();

        ctx.CargoShipments.Add(Marked(Marker));
        ctx.CargoShipments.Add(Marked(Marker));
        await ctx.SaveChangesAsync();

        await LiveDatabaseFixture.DeleteShipmentsAsync(ctx, Marker);

        // DeleteShipmentsAsync kendi içinde de doğruluyor; burada dışarıdan teyit
        var residue = await ctx.CargoShipments.IgnoreQueryFilters()
                               .CountAsync(s => s.Notes == Marker);
        Assert.Equal(0, residue);
    }

    /// <summary>
    /// SOFT-DELETE EDİLMİŞ artık da sayılmalı. Global sorgu filtresi onları
    /// gizler; filtre uygulanırsa temizlik yapılmış GİBİ görünür ve satırlar
    /// tabloda kalmaya devam eder.
    /// </summary>
    [Fact]
    public async Task Artik_kontrolu_soft_delete_edilmis_kayitlari_da_goruyor()
    {
        if (!db.IsAvailable) return;

        await using var ctx = db.CreateContext();

        var soft = Marked(Marker);
        soft.IsDeleted = true;
        soft.DeletedAt = DateTime.UtcNow;
        ctx.CargoShipments.Add(soft);
        await ctx.SaveChangesAsync();

        try
        {
            // Filtre uygulansaydı bu çağrı "temiz" derdi
            await Assert.ThrowsAnyAsync<Exception>(
                () => LiveDatabaseFixture.AssertNoResidueAsync(ctx, Marker));
        }
        finally
        {
            await LiveDatabaseFixture.DeleteShipmentsAsync(ctx, Marker);
        }
    }

    /// <summary>
    /// Önek eşleşmesi: import testi kayıtlarını "marker1", "marker2" gibi
    /// numaralandırıyor; tam eşleşme onları kaçırırdı.
    /// </summary>
    [Fact]
    public async Task Onek_eslesmesi_numaralandirilmis_kayitlari_yakaliyor()
    {
        if (!db.IsAvailable) return;

        await using var ctx = db.CreateContext();

        ctx.CargoShipments.Add(Marked(Marker + "1"));
        ctx.CargoShipments.Add(Marked(Marker + "2"));
        await ctx.SaveChangesAsync();

        // Tam eşleşme bunları görmez → artık kalır
        await LiveDatabaseFixture.DeleteShipmentsAsync(ctx, Marker + "1");

        var kalan = await ctx.CargoShipments.IgnoreQueryFilters()
                             .CountAsync(s => s.Notes != null && s.Notes.StartsWith(Marker));
        Assert.Equal(1, kalan);

        // Önek eşleşmesi hepsini temizler ve doğrular
        await LiveDatabaseFixture.DeleteShipmentsAsync(ctx, Marker, prefix: true);
    }

    /// <summary>
    /// Sayaç geri yükleme: test numara ürettiyse sayaç artar; paylaşılan
    /// veritabanında eski değere dönmeli, yoksa numaralar her koşuda kayar.
    /// </summary>
    [Fact]
    public async Task Sayac_geri_yukleme_eski_degeri_aynen_koyuyor()
    {
        if (!db.IsAvailable) return;

        await using var ctx = db.CreateContext();

        var before = await LiveDatabaseFixture.SnapshotCountersAsync(ctx);
        if (before.Count == 0) return; // sayaç satırı yoksa doğrulanacak bir şey yok

        var direction = before.Keys.First();
        var original  = before[direction];

        await ctx.Database.ExecuteSqlAsync($"""
            UPDATE cargo_number_counters SET "LastValue" = {original + 99}
            WHERE "Direction" = {direction}
            """);

        await LiveDatabaseFixture.RestoreCountersAsync(ctx, before);

        var after = await LiveDatabaseFixture.SnapshotCountersAsync(ctx);
        Assert.Equal(original, after[direction]);
    }
}
