using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// WhatsApp rehberi içe aktarma: telefon doğrulama/normalizasyon, kesin mükerrer
/// (telefon anahtarı), soft-delete geri yükleme ve toplu kayıt.
/// </summary>
public class WhatsAppImportTests
{
    private sealed class FakeImportFileReader : ICargoImportFileReader
    {
        public ImportDocument? Document { get; set; }
        public Task<ImportDocument> ReadAsync(string filePath) => Task.FromResult(Document!);
    }

    private static readonly string[] Headers = ["Ad Soyad", "Telefon", "Firma", "Açıklama"];

    private static ImportDocument Doc(params string?[][] rows) => new()
    {
        SourceName = "kisiler.xlsx",
        Headers    = Headers,
        Rows       = rows.Select((cells, i) => new ImportDocumentRow { RowNumber = i + 2, Cells = cells }).ToList()
    };

    private sealed record Env(
        AnalyzeWhatsAppImportHandler Analyze,
        ImportWhatsAppContactsHandler Import,
        FakeImportFileReader Reader,
        FakeWhatsAppContactRepository Repo,
        FakeAuditLogService Audit,
        FakeUserContext User);

    private static Env Build(bool grantPermission = true)
    {
        var reader = new FakeImportFileReader();
        var repo   = new FakeWhatsAppContactRepository();
        var user   = new FakeUserContext();
        if (grantPermission)
            user.SetUser(user.UserId, user.FullName,
                new HashSet<PermissionType> { PermissionType.CanManageCompanyDirectory });
        var audit     = new FakeAuditLogService();
        var systemLog = new FakeSystemLogService();

        return new Env(
            new AnalyzeWhatsAppImportHandler(reader, repo, user, systemLog, new FakeTextNormalizationService()),
            new ImportWhatsAppContactsHandler(repo, audit, systemLog, user, new FakeTextNormalizationService()),
            reader, repo, audit, user);
    }

    private static AnalyzeWhatsAppImportRequest AnalyzeRequest() => new() { FilePath = "kisiler.xlsx" };

    private static ImportWhatsAppContactsRequest ImportRequest(Env env, params WhatsAppImportRowDto[] rows) => new()
    {
        SourceName = "kisiler.xlsx", Rows = rows, CreatedByUserId = env.User.UserId,
        AnalysisTotalRows = rows.Length, AnalysisValidCount = rows.Length,
        AnalysisWarningCount = 0, AnalysisErrorCount = 0, AnalysisDuplicateCount = 0
    };

    [Fact]
    public async Task YetkisizKullanici_Reddedilir()
    {
        var env = Build(grantPermission: false);
        env.Reader.Document = Doc(["Ali Veli", "0532 123 45 67", null, null]);

        Assert.False((await env.Analyze.HandleAsync(AnalyzeRequest())).Success);
    }

    [Fact]
    public async Task GecerliSatir_TelefonNormalizeEdilir()
    {
        var env = Build();
        env.Reader.Document = Doc(["Ali Veli", "0532 123 45 67", "Acme", "muhasebe"]);

        var result = await env.Analyze.HandleAsync(AnalyzeRequest());

        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal(CargoImportRowStatus.Valid, row.Status);
        Assert.Equal("+905321234567", row.NormalizedPhone);
    }

    [Fact]
    public async Task SabitHatNumarasi_HataVerir()
    {
        var env = Build();
        env.Reader.Document = Doc(["Santral", "0216 337 14 48", null, null]);

        var result = await env.Analyze.HandleAsync(AnalyzeRequest());

        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal(CargoImportRowStatus.Error, row.Status);
        Assert.False(row.CanInclude);
    }

    [Fact]
    public async Task AyniNumara_DosyaIciVeAktifKayitta_KesinMukerrer()
    {
        var env = Build();
        env.Repo.Items.Add(new WhatsAppContact
        {
            Id = Guid.NewGuid(), FullName = "Kayıtlı Kişi", Phone = "+905417775544", IsActive = true
        });
        env.Reader.Document = Doc(
            ["Ali Veli",  "0532 123 45 67", null, null],
            ["Ali Kopya", "532 123 45 67",  null, null],   // dosya içi aynı numara
            ["Veli Can",  "0541 777 55 44", null, null]);  // DB'de aktif kayıt

        var result = await env.Analyze.HandleAsync(AnalyzeRequest());

        var rows = result.Data!.Rows;
        Assert.Equal(CargoImportRowStatus.Valid, rows[0].Status);
        Assert.Equal(DuplicateKind.ExactKeyInFile, rows[1].DuplicateReason!.Kind);
        Assert.False(rows[1].CanInclude);
        Assert.Equal(DuplicateKind.ExactKeyInDatabase, rows[2].DuplicateReason!.Kind);
        Assert.False(rows[2].CanInclude);
    }

    [Fact]
    public async Task SilinmisNumara_UyariIleGecer_ImportGeriYukler()
    {
        var env = Build();
        var deleted = new WhatsAppContact
        {
            Id = Guid.NewGuid(), FullName = "Eski Kayıt", Phone = "+905321234567",
            IsActive = false, IsDeleted = true, DeletedAt = DateTime.UtcNow
        };
        env.Repo.Items.Add(deleted);
        env.Reader.Document = Doc(["Yeni Ad", "0532 123 45 67", "Acme", null]);

        var analysis = await env.Analyze.HandleAsync(AnalyzeRequest());
        var row = Assert.Single(analysis.Data!.Rows);
        Assert.Equal(CargoImportRowStatus.Warning, row.Status);   // geri yükleme uyarısı
        Assert.Equal(deleted.Id, row.ResurrectContactId);
        Assert.True(row.IncludedByDefault);

        var result = await env.Import.HandleAsync(ImportRequest(env, row));

        Assert.True(result.Success);
        Assert.Equal(1, result.Data!.ResurrectedCount);
        Assert.False(deleted.IsDeleted);
        Assert.True(deleted.IsActive);
        Assert.Equal("Yeni Ad", deleted.FullName);
        Assert.Single(env.Audit.Entries, e => e.Action == AuditAction.WhatsAppContactUpdated);
        Assert.Single(env.Audit.Entries, e => e.Action == AuditAction.WhatsAppImportCompleted);
    }

    [Fact]
    public async Task BasariliImport_YeniKayitlarEklenir_AuditYazilir()
    {
        var env = Build();
        var rows = new[]
        {
            new WhatsAppImportRowDto { RowNumber = 2, FullName = "Ali Veli", NormalizedPhone = "+905321234567" },
            new WhatsAppImportRowDto { RowNumber = 3, FullName = "Ayşe Yılmaz", NormalizedPhone = "+905417775544" },
        };

        var result = await env.Import.HandleAsync(ImportRequest(env, rows));

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.ImportedCount);
        Assert.Equal(0, result.Data.ResurrectedCount);
        Assert.Equal(2, env.Repo.Items.Count);
        Assert.Equal(2, env.Audit.Entries.Count(e => e.Action == AuditAction.WhatsAppContactCreated));
    }

    [Fact]
    public async Task OnizlemeSonrasiAyniNumaraAktifKayitOldu_TumIslemIptal()
    {
        var env = Build();
        env.Repo.Items.Add(new WhatsAppContact
        {
            Id = Guid.NewGuid(), FullName = "Araya Giren", Phone = "+905321234567", IsActive = true
        });

        var result = await env.Import.HandleAsync(ImportRequest(env,
            new WhatsAppImportRowDto { RowNumber = 2, FullName = "Ali Veli", NormalizedPhone = "+905321234567" },
            new WhatsAppImportRowDto { RowNumber = 3, FullName = "Ayşe", NormalizedPhone = "+905417775544" }));

        Assert.False(result.Success);
        Assert.Single(env.Repo.Items); // hiçbir yeni kayıt eklenmedi
    }

    [Fact]
    public async Task TransactionHatasi_HicbirKayitOlusmaz()
    {
        var env = Build();
        env.Repo.FailNextSaveImport = true;

        var result = await env.Import.HandleAsync(ImportRequest(env,
            new WhatsAppImportRowDto { RowNumber = 2, FullName = "Ali", NormalizedPhone = "+905321234567" }));

        Assert.False(result.Success);
        Assert.Empty(env.Repo.Items);
        Assert.DoesNotContain(env.Audit.Entries, e => e.Action == AuditAction.WhatsAppImportCompleted);
    }
}
