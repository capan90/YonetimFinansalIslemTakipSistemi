using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>Firma rehberi içe aktarma: analiz, mükerrer tespiti ve toplu kayıt.</summary>
public class DirectoryImportTests
{
    private sealed class FakeImportFileReader : ICargoImportFileReader
    {
        public ImportDocument? Document { get; set; }
        public Task<ImportDocument> ReadAsync(string filePath) => Task.FromResult(Document!);
    }

    private static readonly string[] Headers = ["Firma Adı", "Yetkili Kişi", "Adres", "İl", "Telefon", "Not"];

    private static ImportDocument Doc(params string?[][] rows) => new()
    {
        SourceName = "rehber.xlsx",
        Headers    = Headers,
        Rows       = rows.Select((cells, i) => new ImportDocumentRow { RowNumber = i + 2, Cells = cells }).ToList()
    };

    private sealed record Env(
        AnalyzeDirectoryImportHandler Analyze,
        ImportDirectoryEntriesHandler Import,
        FakeImportFileReader Reader,
        FakeCompanyDirectoryRepository Repo,
        FakeAuditLogService Audit,
        FakeSystemLogService SystemLog,
        FakeUserContext User);

    private static Env Build(bool grantPermission = true)
    {
        var reader = new FakeImportFileReader();
        var repo   = new FakeCompanyDirectoryRepository();
        var user   = new FakeUserContext();
        if (grantPermission) user.GrantAll();
        var audit     = new FakeAuditLogService();
        var systemLog = new FakeSystemLogService();

        return new Env(
            new AnalyzeDirectoryImportHandler(reader, repo, user, systemLog, new FakeTextNormalizationService()),
            new ImportDirectoryEntriesHandler(repo, audit, systemLog, user, new FakeTextNormalizationService()),
            reader, repo, audit, systemLog, user);
    }

    private static AnalyzeDirectoryImportRequest AnalyzeRequest() => new() { FilePath = "rehber.xlsx" };

    [Fact]
    public async Task YetkisizKullanici_AnalizVeImportReddedilir()
    {
        var env = Build(grantPermission: false);
        env.Reader.Document = Doc(["Acme A.Ş.", null, null, null, null, null]);

        Assert.False((await env.Analyze.HandleAsync(AnalyzeRequest())).Success);
        Assert.False((await env.Import.HandleAsync(new ImportDirectoryEntriesRequest
        {
            SourceName = "rehber.xlsx", Rows = [], CreatedByUserId = env.User.UserId,
            AnalysisTotalRows = 0, AnalysisValidCount = 0, AnalysisWarningCount = 0,
            AnalysisErrorCount = 0, AnalysisDuplicateCount = 0
        })).Success);
    }

    [Fact]
    public async Task GecerliSatir_AlanlarParseEdilir()
    {
        var env = Build();
        env.Reader.Document = Doc(["Acme A.Ş.", "Ali Veli", "Sanayi Cad. 5", "İstanbul", "0216 111 11 11", "not"]);

        var result = await env.Analyze.HandleAsync(AnalyzeRequest());

        Assert.True(result.Success);
        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal(CargoImportRowStatus.Valid, row.Status);
        Assert.Equal("Acme A.Ş.", row.CompanyName);
        Assert.Equal("Ali Veli", row.ContactPerson);
        Assert.Equal("0216 111 11 11", row.Phone);
    }

    [Fact]
    public async Task KullaniciBuyukHarfSectiyse_OnizlemeVerisiTercihineGoreDonusur()
    {
        // Gerçek normalizasyon servisiyle: harf tercihi analiz aşamasında uygulanır,
        // önizleme verinin kaydedilecek halini gösterir (telefon/e-posta muaf)
        var reader = new FakeImportFileReader();
        var repo   = new FakeCompanyDirectoryRepository();
        var user   = new FakeUserContext { TextCasePreference = TextCasePreference.Uppercase };
        user.GrantAll();
        var handler = new AnalyzeDirectoryImportHandler(
            reader, repo, user, new FakeSystemLogService(),
            new Application.Services.UserTextNormalizationService(user));

        reader.Document = Doc(["acme lojistik a.ş.", "ali veli", "sanayi cad. 5", "istanbul", "0216 111 11 11", null]);

        var result = await handler.HandleAsync(AnalyzeRequest());

        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal("ACME LOJİSTİK A.Ş.", row.CompanyName); // tr-TR: i→İ
        Assert.Equal("ALİ VELİ", row.ContactPerson);
        Assert.Equal("İSTANBUL", row.City);
        Assert.Equal("0216 111 11 11", row.Phone); // telefon harf dönüşümünden muaf
    }

    [Fact]
    public async Task BosFirmaAdi_Hata()
    {
        var env = Build();
        env.Reader.Document = Doc(["  ", "Ali", null, null, null, null]);

        var result = await env.Analyze.HandleAsync(AnalyzeRequest());

        Assert.Equal(CargoImportRowStatus.Error, Assert.Single(result.Data!.Rows).Status);
    }

    [Fact]
    public async Task UzunTelefon_NotaTasinir_TelefonBosKalir()
    {
        var env = Build();
        var messyPhone = "211 21 21-(22 42 (EMRE BEY ODEME)-(24 61 EBRU HN) 0532 111 22 33";
        env.Reader.Document = Doc(["Acme A.Ş.", null, null, null, messyPhone, "mevcut not"]);

        var result = await env.Analyze.HandleAsync(AnalyzeRequest());

        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal(CargoImportRowStatus.Warning, row.Status);
        Assert.Null(row.Phone);
        Assert.Contains(messyPhone, row.Notes);
        Assert.Contains("mevcut not", row.Notes);
    }

    [Fact]
    public async Task AyniAd_FarkliTelefon_MukerrerSayilmaz()
    {
        // İş gerçeği: bir firmanın muhasebe/depo gibi birden çok hattı olabilir —
        // aynı ad farklı numarayla ayrı kayıt olarak içe aktarılır
        var env = Build();
        env.Repo.Items.Add(new CompanyDirectory
        {
            Id = Guid.NewGuid(), CompanyName = "Acme A.Ş.", AddressLine = "-",
            Phone = "0216 111 11 11", IsActive = true
        });
        env.Reader.Document = Doc(
            ["Acme A.Ş.", "Muhasebe", null, null, "0216 222 22 22", null],  // DB'dekiyle aynı ad, farklı no
            ["Acme A.Ş.", "Depo",     null, null, "0216 333 33 33", null],  // dosya içi aynı ad, farklı no
            ["Acme A.Ş.", null,       null, null, "02161111111",    null]); // DB'dekiyle aynı ad + AYNI no (biçim farklı)

        var result = await env.Analyze.HandleAsync(AnalyzeRequest());

        var rows = result.Data!.Rows;
        Assert.Equal(CargoImportRowStatus.Valid, rows[0].Status);
        Assert.Equal(CargoImportRowStatus.Valid, rows[1].Status);
        Assert.Equal(CargoImportRowStatus.Duplicate, rows[2].Status); // rakam bazlı eşleşme
    }

    [Fact]
    public async Task DosyaIciVeDbMukerrer_NormalizeAdlaYakalanir()
    {
        var env = Build();
        env.Repo.Items.Add(new CompanyDirectory
        {
            Id = Guid.NewGuid(), CompanyName = "Mevcut Firma", AddressLine = "-", IsActive = true
        });
        env.Reader.Document = Doc(
            ["Acme A.Ş.", null, null, null, null, null],
            ["ACME  a.ş.", null, null, null, null, null],   // dosya içi mükerrer (normalize)
            ["MEVCUT FİRMA", null, null, null, null, null]); // DB mükerreri

        var result = await env.Analyze.HandleAsync(AnalyzeRequest());

        var rows = result.Data!.Rows;
        Assert.Equal(CargoImportRowStatus.Valid, rows[0].Status);
        Assert.Equal(DuplicateKind.SimilarInFile, rows[1].DuplicateReason!.Kind);
        Assert.Equal(DuplicateKind.SimilarInDatabase, rows[2].DuplicateReason!.Kind);
        // Olası mükerrer: kullanıcı bilinçli dahil edebilir (şube senaryosu)
        Assert.True(rows[1].CanInclude);
        Assert.False(rows[1].IncludedByDefault);
    }

    [Fact]
    public async Task BasariliImport_BosAdresYerTutucuylaKaydedilir_AuditYazilir()
    {
        var env = Build();
        var rows = new List<DirectoryImportRowDto>
        {
            new() { RowNumber = 2, CompanyName = "Acme A.Ş.", Phone = "0216 1 11" },
            new() { RowNumber = 3, CompanyName = "İnci Ltd.", AddressLine = "Cadde 1" },
        };

        var result = await env.Import.HandleAsync(new ImportDirectoryEntriesRequest
        {
            SourceName = "rehber.xlsx", Rows = rows, CreatedByUserId = env.User.UserId,
            AnalysisTotalRows = 2, AnalysisValidCount = 2, AnalysisWarningCount = 0,
            AnalysisErrorCount = 0, AnalysisDuplicateCount = 0
        });

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.ImportedCount);
        Assert.Equal(2, env.Repo.Items.Count);
        // Adres import'ta zorunlu değil — DB zorunluluğu görünür yer tutucuyla karşılanır
        Assert.Equal("-", env.Repo.Items[0].AddressLine);
        Assert.Equal("Cadde 1", env.Repo.Items[1].AddressLine);

        Assert.Equal(2, env.Audit.Entries.Count(e => e.Action == AuditAction.CompanyDirectoryCreated));
        Assert.Single(env.Audit.Entries, e => e.Action == AuditAction.DirectoryImportCompleted);
    }

    [Fact]
    public async Task OnizlemeSonrasiAyniAdEklendi_TumIslemIptal()
    {
        var env = Build();
        env.Repo.Items.Add(new CompanyDirectory
        {
            Id = Guid.NewGuid(), CompanyName = "Acme A.Ş.", AddressLine = "-", IsActive = true
        });

        // Satır, önizlemede mükerrer işaretlenmemişti (DuplicateReason yok) ama DB'de artık var
        var result = await env.Import.HandleAsync(new ImportDirectoryEntriesRequest
        {
            SourceName = "rehber.xlsx",
            Rows = [new DirectoryImportRowDto { RowNumber = 2, CompanyName = "acme a.ş." }],
            CreatedByUserId = env.User.UserId,
            AnalysisTotalRows = 1, AnalysisValidCount = 1, AnalysisWarningCount = 0,
            AnalysisErrorCount = 0, AnalysisDuplicateCount = 0
        });

        Assert.False(result.Success);
        Assert.Single(env.Repo.Items); // yeni kayıt eklenmedi
    }

    [Fact]
    public async Task TransactionHatasi_HicbirKayitOlusmaz()
    {
        var env = Build();
        env.Repo.FailNextAddRange = true;

        var result = await env.Import.HandleAsync(new ImportDirectoryEntriesRequest
        {
            SourceName = "rehber.xlsx",
            Rows = [new DirectoryImportRowDto { RowNumber = 2, CompanyName = "Acme A.Ş." }],
            CreatedByUserId = env.User.UserId,
            AnalysisTotalRows = 1, AnalysisValidCount = 1, AnalysisWarningCount = 0,
            AnalysisErrorCount = 0, AnalysisDuplicateCount = 0
        });

        Assert.False(result.Success);
        Assert.Empty(env.Repo.Items);
        Assert.DoesNotContain(env.Audit.Entries, e => e.Action == AuditAction.DirectoryImportCompleted);
    }
}
