using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using static YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Import.CashImportColumnMap;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Finans içe aktarma: GİREN/ÇIKAN kuralları, tutar/para birimi parse,
/// mükerrer tespiti ve ya-hep-ya-hiç toplu kayıt.
/// </summary>
public class CashImportTests
{
    private sealed class FakeImportFileReader : ICargoImportFileReader
    {
        public ImportDocument? Document { get; set; }
        public Task<ImportDocument> ReadAsync(string filePath) => Task.FromResult(Document!);
    }

    // Kullanıcının gerçek dosyasındaki başlıklar: NO/AY/BAKİYE yok sayılmalı
    private static readonly string[] Headers =
        ["NO", "AY", "TARİH", "AÇIKLAMA", "GİREN", "ÇIKAN", "BAKİYE", "Para Birimi"];

    private static ImportDocument Doc(params string?[][] rows) => new()
    {
        SourceName = "finans.xlsx",
        Headers    = Headers,
        Rows       = rows.Select((cells, i) => new ImportDocumentRow { RowNumber = i + 2, Cells = cells }).ToList()
    };

    private static string? Dun => DateTime.Today.AddDays(-1).ToString("dd.MM.yyyy");

    private sealed record Env(
        AnalyzeCashImportHandler Analyze,
        ImportCashTransactionsHandler Import,
        FakeImportFileReader Reader,
        FakeCashTransactionRepository Repo,
        FakeAuditLogService Audit,
        FakeUserContext User);

    private static Env Build(bool grantPermission = true)
    {
        var reader = new FakeImportFileReader();
        var repo   = new FakeCashTransactionRepository();
        var user   = new FakeUserContext();
        if (grantPermission)
            user.SetUser(user.UserId, user.FullName,
                new HashSet<PermissionType> { PermissionType.CanCreateTransaction });
        var audit     = new FakeAuditLogService();
        var systemLog = new FakeSystemLogService();

        return new Env(
            new AnalyzeCashImportHandler(reader, repo, user, systemLog, new FakeTextNormalizationService()),
            new ImportCashTransactionsHandler(repo, audit, systemLog, user),
            reader, repo, audit, user);
    }

    private static AnalyzeCashImportRequest AnalyzeRequest() => new() { FilePath = "finans.xlsx" };

    // ── Parse birim testleri ────────────────────────────────────────────────

    [Theory]
    [InlineData("10", 10)]
    [InlineData("10.5", 10.5)]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1234,56", 1234.56)]
    [InlineData("150 ", 150)]
    public void TutarParse_TurkceVeInvariantBicimler(string input, double expected)
        => Assert.Equal((decimal)expected, ParseAmount(input));

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    public void TutarParse_Gecersiz_Null(string input)
        => Assert.Null(ParseAmount(input));

    [Theory]
    [InlineData(null, CurrencyType.TRY)]
    [InlineData("", CurrencyType.TRY)]
    [InlineData("TL", CurrencyType.TRY)]
    [InlineData("Dolar", CurrencyType.USD)]
    [InlineData("EUR", CurrencyType.EUR)]
    public void ParaBirimi_TurkceEtiketler(string? label, CurrencyType expected)
        => Assert.Equal(expected, ParseCurrency(label));

    [Fact]
    public void ParaBirimi_Taninmayan_Null()
        => Assert.Null(ParseCurrency("Sterlin"));

    // ── Analiz ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GercekDosyaBasliklari_EslesirVeFazlalikYokSayilir()
    {
        var env = Build();
        env.Reader.Document = Doc(["1", "OCAK", Dun, "MARKET", "", "10", "-10", ""]);

        var result = await env.Analyze.HandleAsync(AnalyzeRequest());

        Assert.True(result.Success, result.ErrorMessage ?? "");
        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal(CargoImportRowStatus.Valid, row.Status);
        Assert.Equal(TransactionType.Cikis, row.TransactionType); // ÇIKAN dolu
        Assert.Equal(10m, row.Amount);
        Assert.Equal(CurrencyType.TRY, row.CurrencyType);          // Para Birimi boş → TL
        Assert.Contains("NO", result.Data.IgnoredColumns);
        Assert.Contains("BAKİYE", result.Data.IgnoredColumns);
    }

    [Fact]
    public async Task GirenDolu_GirisIslemiOlur()
    {
        var env = Build();
        env.Reader.Document = Doc(["1", null, Dun, "TAHSİLAT", "250,50", null, null, "USD"]);

        var result = await env.Analyze.HandleAsync(AnalyzeRequest());

        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal(TransactionType.Giris, row.TransactionType);
        Assert.Equal(250.50m, row.Amount);
        Assert.Equal(CurrencyType.USD, row.CurrencyType);
    }

    [Theory]
    [InlineData("10", "5")]   // ikisi birden dolu
    [InlineData(null, null)]  // ikisi de boş
    [InlineData("0", null)]   // sıfır tutar
    [InlineData("-5", null)]  // negatif
    public async Task GirenCikanKurallari_HataUretir(string? giren, string? cikan)
    {
        var env = Build();
        env.Reader.Document = Doc(["1", null, Dun, "TEST", giren, cikan, null, null]);

        var result = await env.Analyze.HandleAsync(AnalyzeRequest());

        Assert.Equal(CargoImportRowStatus.Error, Assert.Single(result.Data!.Rows).Status);
    }

    [Fact]
    public async Task IleriTarih_FinansKuraliGeregiHata()
    {
        var env = Build();
        env.Reader.Document = Doc(["1", null, DateTime.Today.AddDays(1).ToString("dd.MM.yyyy"), "TEST", "10", null, null, null]);

        var result = await env.Analyze.HandleAsync(AnalyzeRequest());

        Assert.Equal(CargoImportRowStatus.Error, Assert.Single(result.Data!.Rows).Status);
    }

    [Fact]
    public async Task BosAciklama_Hata()
    {
        var env = Build();
        env.Reader.Document = Doc(["1", null, Dun, "  ", "10", null, null, null]);

        var result = await env.Analyze.HandleAsync(AnalyzeRequest());

        Assert.Equal(CargoImportRowStatus.Error, Assert.Single(result.Data!.Rows).Status);
    }

    [Fact]
    public async Task MukerrerTespiti_DosyaIciVeDb()
    {
        var env = Build();
        env.Repo.Items.Add(new CashTransaction
        {
            Id = Guid.NewGuid(),
            TransactionDate = DateTime.Today.AddDays(-1),
            TransactionType = TransactionType.Cikis,
            CurrencyType = CurrencyType.TRY,
            Amount = 10m,
            Description = "MARKET"
        });
        env.Reader.Document = Doc(
            ["1", null, Dun, "MARKET", null, "10", null, null],   // DB mükerreri
            ["2", null, Dun, "KIRTASIYE", null, "20", null, null],
            ["3", null, Dun, "KIRTASIYE", null, "20", null, null]); // dosya içi mükerrer

        var result = await env.Analyze.HandleAsync(AnalyzeRequest());

        var rows = result.Data!.Rows;
        Assert.Equal(DuplicateKind.SimilarInDatabase, rows[0].DuplicateReason!.Kind);
        Assert.Equal(CargoImportRowStatus.Valid, rows[1].Status);
        Assert.Equal(DuplicateKind.SimilarInFile, rows[2].DuplicateReason!.Kind);
        // Olası mükerrer: kullanıcı bilinçli dahil edebilir (aynı gün iki meşru işlem olabilir)
        Assert.True(rows[0].CanInclude);
        Assert.False(rows[0].IncludedByDefault);
    }

    // ── Import ──────────────────────────────────────────────────────────────

    private static CashImportRowDto Row(decimal amount, TransactionType type = TransactionType.Cikis,
        string description = "MARKET", int rowNumber = 2) => new()
    {
        RowNumber       = rowNumber,
        TransactionDate = DateTime.Today.AddDays(-1),
        TransactionType = type,
        CurrencyType    = CurrencyType.TRY,
        Amount          = amount,
        Description     = description
    };

    private static ImportCashTransactionsRequest Request(Env env, params CashImportRowDto[] rows) => new()
    {
        SourceName = "finans.xlsx", Rows = rows, CreatedByUserId = env.User.UserId,
        AnalysisTotalRows = rows.Length, AnalysisValidCount = rows.Length,
        AnalysisWarningCount = 0, AnalysisErrorCount = 0, AnalysisDuplicateCount = 0
    };

    [Fact]
    public async Task YetkisizKullanici_Reddedilir()
    {
        var env = Build(grantPermission: false);

        var result = await env.Import.HandleAsync(Request(env, Row(10)));

        Assert.False(result.Success);
        Assert.Empty(env.Repo.Items);
    }

    [Fact]
    public async Task BasariliImport_KayitVeAuditDogru()
    {
        var env = Build();

        var result = await env.Import.HandleAsync(Request(env,
            Row(100, TransactionType.Giris, "TAHSİLAT", 2),
            Row(40,  TransactionType.Cikis, "MARKET", 3)));

        Assert.True(result.Success, result.ErrorMessage ?? "");
        Assert.Equal(2, result.Data!.ImportedCount);
        Assert.Equal(1, result.Data.GirisCount);
        Assert.Equal(1, result.Data.CikisCount);
        Assert.Equal(2, env.Repo.Items.Count);
        Assert.All(env.Repo.Items, t => Assert.Equal(DateTimeKind.Utc, t.TransactionDate.Kind));

        Assert.Equal(2, env.Audit.Entries.Count(e => e.Action == AuditAction.TransactionCreated));
        Assert.Single(env.Audit.Entries, e => e.Action == AuditAction.CashImportCompleted);
    }

    [Fact]
    public async Task OnizlemeSonrasiAyniIslemGirildi_TumIslemIptal()
    {
        var env = Build();
        env.Repo.Items.Add(new CashTransaction
        {
            Id = Guid.NewGuid(),
            TransactionDate = DateTime.Today.AddDays(-1),
            TransactionType = TransactionType.Cikis,
            CurrencyType = CurrencyType.TRY,
            Amount = 10m,
            Description = "MARKET"
        });

        var result = await env.Import.HandleAsync(Request(env, Row(10), Row(99, description: "BAŞKA", rowNumber: 3)));

        Assert.False(result.Success);
        Assert.Single(env.Repo.Items); // yeni kayıt eklenmedi
    }

    [Fact]
    public async Task TransactionHatasi_HicbirKayitOlusmaz()
    {
        var env = Build();
        env.Repo.FailNextAddRange = true;

        var result = await env.Import.HandleAsync(Request(env, Row(10)));

        Assert.False(result.Success);
        Assert.Empty(env.Repo.Items);
        Assert.DoesNotContain(env.Audit.Entries, e => e.Action == AuditAction.CashImportCompleted);
    }

    [Fact]
    public async Task HataliSatirSizarsa_Reddedilir()
    {
        var env = Build();
        var bad = Row(10);
        bad.AddError("Tarih", "Geçersiz");
        bad.ResolveStatus();

        var result = await env.Import.HandleAsync(Request(env, bad));

        Assert.False(result.Success);
        Assert.Empty(env.Repo.Items);
    }
}
