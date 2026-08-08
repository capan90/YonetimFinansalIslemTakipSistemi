using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Tests.Integration;

/// <summary>
/// Canlı PostgreSQL'e karşı çalışan testlerin ortak zemini (Faz F2).
///
/// NEDEN VAR — üç sorun vardı:
///
///   1) HER TEST DB'Yİ AYRI SONDALIYORDU. Her metot TryCreateContext ve
///      TryCreateFactory çağırıyor, ikisi de ayrı ayrı bağlantı açıp
///      kapatıyordu. Sonda maliyeti bir yana, "DB var mı" sorusu test
///      başına farklı cevaplanabiliyordu.
///
///   2) TEMİZLİK DOĞRULANMIYORDU. Testler kendi kayıtlarını siliyordu ama
///      silmenin İŞE YARADIĞINI kimse kontrol etmiyordu. Bir DELETE
///      eşleşmezse artık satırlar dev veritabanında birikir ve BİR SONRAKİ
///      koşunun sayımlarını bozar — kaynağı görünmeyen flaky.
///
///   3) SAYAÇ GERİ YÜKLEME KOPYALANMIŞTI. İki test sınıfı aynı snapshot/
///      restore mantığını ayrı ayrı taşıyordu; biri düzeltilse diğeri eski
///      kalırdı.
///
/// Fixture koleksiyon başına BİR kez kurulur (ICollectionFixture) ve
/// koleksiyon paralelsizdir — iki cargo sınıfı aynı tabloları eşzamanlı
/// kullandığında sayaç assert'leri karışıyordu.
/// </summary>
public sealed class LiveDatabaseFixture
{
    public LiveDatabaseFixture()
    {
        Factory = IntegrationDb.TryCreateFactory();
    }

    /// <summary>Testler boyunca kullanılan tek factory; DB yoksa null.</summary>
    public IDbContextFactory<AppDbContext>? Factory { get; }

    /// <summary>
    /// Canlı DB erişilebilir mi. Erişilemiyorsa integration testleri işlem
    /// yapmadan geçer (offline/CI kırılmasın) — bu bilinçli bir tercih ama
    /// <see cref="SkipReason"/> ile görünür kalır.
    /// </summary>
    public bool IsAvailable => Factory is not null;

    public string SkipReason =>
        "Canlı PostgreSQL erişilemiyor (YONETIM_DB_CONNECTION veya " +
        "appsettings.Development.json) — integration doğrulaması ÇALIŞMADI.";

    public AppDbContext CreateContext() =>
        Factory?.CreateDbContext() ?? throw new InvalidOperationException(SkipReason);

    // ── Sayaç anlık görüntüsü ────────────────────────────────────────────

    /// <summary>
    /// Kargo numarası sayaçlarının test öncesi hâli. Testler numara üretince
    /// sayaç artar; dev veritabanı paylaşıldığı için eski değere döndürülmeli.
    /// </summary>
    public static async Task<Dictionary<int, long>> SnapshotCountersAsync(AppDbContext ctx) =>
        await ctx.CargoNumberCounters.AsNoTracking()
                 .ToDictionaryAsync(c => (int)c.Direction, c => c.LastValue);

    public static async Task RestoreCountersAsync(AppDbContext ctx, Dictionary<int, long> counters)
    {
        foreach (var (direction, value) in counters)
            await ctx.Database.ExecuteSqlAsync($"""
                UPDATE cargo_number_counters SET "LastValue" = {value}
                WHERE "Direction" = {direction}
                """);
    }

    // ── Temizlik ─────────────────────────────────────────────────────────

    /// <summary>
    /// Testin ürettiği kargo kayıtlarını siler ve SİLİNDİĞİNİ DOĞRULAR.
    ///
    /// Doğrulama şart: sessizce eşleşmeyen bir DELETE, dev veritabanında
    /// artık satır bırakır. O satırlar bir sonraki koşunun sayımlarına
    /// karışır ve hata testin kendisinde değil, SONRAKİ testte görünür.
    /// </summary>
    /// <param name="marker">Kayıtların Notes alanına yazılan işaret.</param>
    /// <param name="prefix">true ise LIKE 'marker%', false ise tam eşleşme.</param>
    public static async Task DeleteShipmentsAsync(AppDbContext ctx, string marker, bool prefix = false)
    {
        if (prefix)
        {
            var pattern = marker + "%";
            await ctx.Database.ExecuteSqlAsync($"""DELETE FROM cargo_shipments WHERE "Notes" LIKE {pattern}""");
        }
        else
        {
            await ctx.Database.ExecuteSqlAsync($"""DELETE FROM cargo_shipments WHERE "Notes" = {marker}""");
        }

        await AssertNoResidueAsync(ctx, marker, prefix);
    }

    /// <summary>
    /// İşaretli hiçbir satır kalmadığını doğrular. Test verisi izolasyonunun
    /// tek ölçülebilir kanıtı budur.
    /// </summary>
    public static async Task AssertNoResidueAsync(AppDbContext ctx, string marker, bool prefix = false)
    {
        // IgnoreQueryFilters: soft-delete edilmiş artıklar da sayılmalı;
        // global filtre onları gizler ve temizlik yapılmış gibi görünürdü.
        var pattern = prefix ? marker + "%" : marker;

        var residue = prefix
            ? await ctx.CargoShipments.IgnoreQueryFilters()
                       .CountAsync(s => s.Notes != null && s.Notes.StartsWith(pattern))
            : await ctx.CargoShipments.IgnoreQueryFilters()
                       .CountAsync(s => s.Notes == marker);

        Assert.True(residue == 0,
            $"Test verisi temizlenemedi: '{marker}' işaretli {residue} kargo kaydı duruyor. " +
            "Bu kayıtlar sonraki koşunun sayımlarını bozar.");
    }
}

/// <summary>
/// Canlı PostgreSQL'e karşı çalışan integration testleri aynı collection'da
/// toplar ve PARALELSİZLEŞTİRİR.
///
/// xUnit collection'ları varsayılan olarak paralel çalıştırır; iki cargo
/// integration sınıfı aynı tabloları (cargo_shipments, cargo_number_counters)
/// eşzamanlı kullanınca sayaç/sayım assert'leri ara sıra karışıyordu (flaky).
/// Aynı collection + DisableParallelization ile seri çalışırlar.
///
/// ICollectionFixture: DB sondası koleksiyon başına bir kez yapılır.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class LiveDatabaseCollection : ICollectionFixture<LiveDatabaseFixture>
{
    public const string Name = "LiveDatabase";
}
