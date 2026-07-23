using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Toplu içe aktarma handler'ı: yetki, yeniden doğrulama, ya-hep-ya-hiç transaction,
/// sıralı numara üretimi, audit ve dashboard cache invalidation.
/// </summary>
public class CargoImportHandlerTests
{
    private sealed record Env(
        ImportCargoShipmentsHandler Handler,
        FakeCargoShipmentRepository Shipments,
        FakeCompanyDirectoryRepository Directories,
        FakeAuditLogService Audit,
        FakeSystemLogService SystemLog,
        FakeCargoDashboardCache Cache,
        FakeUserContext User,
        Guid AcmeId);

    private static Env Build(bool grantPermission = true)
    {
        var shipments   = new FakeCargoShipmentRepository();
        var directories = new FakeCompanyDirectoryRepository();
        var cargoRepo   = new FakeCargoCompanyRepository();
        var audit       = new FakeAuditLogService();
        var systemLog   = new FakeSystemLogService();
        var cache       = new FakeCargoDashboardCache();
        var user        = new FakeUserContext();
        if (grantPermission) user.GrantAll();

        var acmeId = Guid.NewGuid();
        directories.Items.Add(new CompanyDirectory
        {
            Id = acmeId, CompanyName = "Acme A.Ş.", AddressLine = "Adres", IsActive = true
        });

        var handler = new ImportCargoShipmentsHandler(
            shipments, directories, cargoRepo, audit, systemLog, cache, user,
            new FakeTextNormalizationService());

        return new Env(handler, shipments, directories, audit, systemLog, cache, user, acmeId);
    }

    private static CargoImportRowDto Row(Guid companyId, int rowNumber = 2, string? tracking = null) => new()
    {
        RowNumber          = rowNumber,
        ShipmentDate       = new DateTime(2026, 6, 15),
        CompanyDirectoryId = companyId,
        CompanyName        = "Acme A.Ş.",
        TrackingNumber     = tracking,
        ReceiverCompanyNameSnapshot = "Acme A.Ş."
    };

    private static ImportCargoShipmentsRequest Request(Env env, params CargoImportRowDto[] rows) => new()
    {
        Direction              = CargoShipmentDirection.Outgoing,
        SourceName             = "test.xlsx",
        Rows                   = rows,
        CreatedByUserId        = env.User.UserId,
        AnalysisTotalRows      = rows.Length,
        AnalysisValidCount     = rows.Length,
        AnalysisWarningCount   = 0,
        AnalysisErrorCount     = 0,
        AnalysisDuplicateCount = 0
    };

    [Fact]
    public async Task YetkisizKullanici_Reddedilir_KayitOlusmaz()
    {
        var env = Build(grantPermission: false);

        var result = await env.Handler.HandleAsync(Request(env, Row(env.AcmeId)));

        Assert.False(result.Success);
        Assert.Empty(env.Shipments.Added);
    }

    [Fact]
    public async Task BasariliImport_SiraliNumaralar_AuditVeCacheDogru()
    {
        var env = Build();
        var rows = Enumerable.Range(2, 5).Select(i => Row(env.AcmeId, i)).ToArray();

        var result = await env.Handler.HandleAsync(Request(env, rows));

        Assert.True(result.Success);
        var r = result.Data!;
        Assert.Equal(5, r.ImportedCount);
        Assert.Equal("GDN00001", r.FirstShipmentNumber);
        Assert.Equal("GDN00005", r.LastShipmentNumber);

        // Numaralar boşluksuz ve sıralı; kaynak ExcelImport işaretli
        var added = env.Shipments.Added.OrderBy(s => s.ShipmentNumber).ToList();
        Assert.Equal(5, added.Count);
        Assert.All(added, s => Assert.Equal(CargoShipmentCreatedFrom.ExcelImport, s.CreatedFrom));
        Assert.All(added, s => Assert.Equal(CargoShipmentStatus.Prepared, s.Status)); // giden varsayılanı

        // Satır başına create audit + 1 özet audit
        Assert.Equal(5, env.Audit.Entries.Count(e => e.Action == AuditAction.CargoShipmentCreated));
        var summary = Assert.Single(env.Audit.Entries, e => e.Action == AuditAction.CargoImportCompleted);
        Assert.Contains("GDN00001", summary.NewValues);
        Assert.Contains("test.xlsx", summary.NewValues);

        Assert.Equal(1, env.Cache.InvalidateCount);
    }

    [Fact]
    public async Task GelenKargo_WaitingDurumuylaBaslar()
    {
        var env = Build();
        var request = new ImportCargoShipmentsRequest
        {
            Direction              = CargoShipmentDirection.Incoming,
            SourceName             = "gelen.xlsx",
            Rows                   = [Row(env.AcmeId)],
            CreatedByUserId        = env.User.UserId,
            AnalysisTotalRows      = 1,
            AnalysisValidCount     = 1,
            AnalysisWarningCount   = 0,
            AnalysisErrorCount     = 0,
            AnalysisDuplicateCount = 0
        };

        var result = await env.Handler.HandleAsync(request);

        Assert.True(result.Success);
        var entity = Assert.Single(env.Shipments.Added);
        Assert.Equal(CargoShipmentStatus.Waiting, entity.Status);
        Assert.StartsWith("GLN", entity.ShipmentNumber);
    }

    [Fact]
    public async Task HataliSatirSizarsa_TopluIslemReddedilir()
    {
        var env = Build();
        var bad = Row(env.AcmeId);
        bad.AddError("Tarih", "Geçersiz");
        bad.ResolveStatus();

        var result = await env.Handler.HandleAsync(Request(env, Row(env.AcmeId), bad));

        Assert.False(result.Success);
        Assert.Empty(env.Shipments.Added);
    }

    [Fact]
    public async Task SilinmisFirma_YenidenDogrulamadaYakalanir()
    {
        var env = Build();
        var row = Row(env.AcmeId);
        env.Directories.Items[0].IsActive = false; // önizleme sonrası firma pasifleşti

        var result = await env.Handler.HandleAsync(Request(env, row));

        Assert.False(result.Success);
        Assert.Contains("yeniden analiz", result.ErrorMessage);
        Assert.Empty(env.Shipments.Added);
    }

    [Fact]
    public async Task OnizlemeSonrasiTakipNoCakismasi_TumIslemIptal()
    {
        var env = Build();
        // Önizlemeden sonra başka kullanıcı aynı takip numarasıyla kayıt ekledi
        env.Shipments.Existing.Add(new CargoShipment
        {
            Id = Guid.NewGuid(), Direction = CargoShipmentDirection.Outgoing,
            TrackingNumber = "TRK1", ShipmentNumber = "GDN00099",
            ShipmentDate = DateTime.UtcNow.Date
        });

        var result = await env.Handler.HandleAsync(
            Request(env, Row(env.AcmeId, 2, "trk1"), Row(env.AcmeId, 3)));

        Assert.False(result.Success);
        Assert.Empty(env.Shipments.Added); // hiçbir kayıt oluşmadı
    }

    [Fact]
    public async Task TransactionHatasi_HicbirKayitOlusmaz_SistemLogaYazilir()
    {
        var env = Build();
        env.Shipments.FailNextAddRange = true;

        var result = await env.Handler.HandleAsync(Request(env, Row(env.AcmeId)));

        Assert.False(result.Success);
        Assert.Contains("hiçbir satır", result.ErrorMessage);
        Assert.Empty(env.Shipments.Added);
        Assert.Contains(env.SystemLog.Entries, e => e.Level == "Error" && e.Category == "CargoImport");
        Assert.DoesNotContain(env.Audit.Entries, e => e.Action == AuditAction.CargoImportCompleted);
    }

    [Fact]
    public async Task TekilVeTopluEkleme_AyniSayaciPaylasir()
    {
        var env = Build();

        // Önce manuel bir kayıt numara alır, ardından toplu import devam eden aralığı kullanır
        var manual = new CargoShipment
        {
            Id = Guid.NewGuid(), Direction = CargoShipmentDirection.Outgoing,
            ShipmentDate = DateTime.UtcNow.Date
        };
        await env.Shipments.AddWithAutoNumberAsync(manual);
        Assert.Equal("GDN00001", manual.ShipmentNumber);

        var result = await env.Handler.HandleAsync(Request(env, Row(env.AcmeId, 2), Row(env.AcmeId, 3)));

        Assert.True(result.Success);
        Assert.Equal("GDN00002", result.Data!.FirstShipmentNumber);
        Assert.Equal("GDN00003", result.Data.LastShipmentNumber);
    }
}
