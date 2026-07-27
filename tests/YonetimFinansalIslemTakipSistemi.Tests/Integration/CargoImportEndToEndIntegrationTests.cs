using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

namespace YonetimFinansalIslemTakipSistemi.Tests.Integration;

/// <summary>
/// UÇTAN UCA: gerçek xlsx dosyası → ExcelCargoImportReader → AnalyzeCargoImportHandler
/// → ImportCargoShipmentsHandler → gerçek dev PostgreSQL (sayaç, transaction, unique index).
/// DB erişilemiyorsa test işlem yapmadan geçer. Test kendi verisini oluşturur ve temizler.
/// </summary>
[Collection("LiveDatabase")] // CargoShipmentRepositoryIntegrationTests ile seri çalışır (flaky önlenir)
public class CargoImportEndToEndIntegrationTests
{
    private const string TestMarker = "__import_e2e_test__";

    private static async Task<Dictionary<int, long>> SnapshotCountersAsync(AppDbContext ctx)
        => await ctx.CargoNumberCounters.AsNoTracking()
            .ToDictionaryAsync(c => (int)c.Direction, c => c.LastValue);

    private static async Task CleanupAsync(AppDbContext ctx, Dictionary<int, long> counters,
        Guid directoryId, Guid cargoCompanyId)
    {
        await ctx.Database.ExecuteSqlAsync(
            $"DELETE FROM cargo_shipments WHERE \"Notes\" LIKE {TestMarker + "%"}");
        await ctx.Database.ExecuteSqlAsync(
            $"DELETE FROM company_directories WHERE \"Id\" = {directoryId}");
        await ctx.Database.ExecuteSqlAsync(
            $"DELETE FROM cargo_companies WHERE \"Id\" = {cargoCompanyId}");
        foreach (var (direction, value) in counters)
            await ctx.Database.ExecuteSqlAsync($@"
                UPDATE cargo_number_counters SET ""LastValue"" = {value}
                WHERE ""Direction"" = {direction}");
    }

    /// <summary>Gerçekçi bir Excel dosyası üretir: 3 geçerli, 1 bilinmeyen firma, 1 mükerrer takip no, 1 boş satır.</summary>
    private static string CreateSampleExcel(string directoryName, string cargoCompanyName)
    {
        var path = Path.Combine(Path.GetTempPath(), $"kargo-e2e-{Guid.NewGuid():N}.xlsx");
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Kargo");

        string[] headers = ["Tarih", "Firma", "Kargo Firması", "Gönderi Türü", "Öncelik", "Takip No", "Not"];
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        void Row(int r, object tarih, string firma, string? kargo, string? tur, string? oncelik, string? takip, string not)
        {
            if (tarih is DateTime dt) ws.Cell(r, 1).Value = dt; else ws.Cell(r, 1).Value = (string)tarih;
            ws.Cell(r, 2).Value = firma;
            if (kargo   is not null) ws.Cell(r, 3).Value = kargo;
            if (tur     is not null) ws.Cell(r, 4).Value = tur;
            if (oncelik is not null) ws.Cell(r, 5).Value = oncelik;
            if (takip   is not null) ws.Cell(r, 6).Value = takip;
            ws.Cell(r, 7).Value = not;
        }

        // 2-4: geçerli — tarihler farklı ki olası-mükerrer anahtarı (tarih+firma+kargo)
        // yanlış pozitif üretmesin; tarih hücresi hem gerçek DateTime hem metin denenir
        Row(2, DateTime.Today,                directoryName, cargoCompanyName, "Evrak",  "Acil",   "E2E-TRK-1", TestMarker + "1");
        Row(3, DateTime.Today.AddDays(-1).ToString("dd.MM.yyyy"), directoryName, null, "Numune", "Normal", null, TestMarker + "2");
        // tr-TR büyütme (i→İ): gerçek kullanıcının BÜYÜK HARF yazımını simüle eder;
        // invariant büyütme i→I yapar ve Türkçe eşleşmeyi bilerek bozar (ayrı senaryo)
        Row(4, DateTime.Today.AddDays(-2),
            directoryName.ToUpper(System.Globalization.CultureInfo.GetCultureInfo("tr-TR")),
            cargoCompanyName, null, "Orta", null, TestMarker + "3");
        // 5: bilinmeyen firma → Error
        Row(5, DateTime.Today,                "Böyle Bir Firma Yok Ltd.", null, null, null, null, TestMarker + "err");
        // 6: 2. satırla aynı takip no → kesin mükerrer
        Row(6, DateTime.Today,                directoryName, null, null, null, "e2e-trk-1 ", TestMarker + "dup");
        // 7: tamamen boş satır → atlanır
        // 8: boş satırdan sonra veri (LastRowUsed kapsasın diye 8'e bir satır daha)
        Row(8, DateTime.Today.AddDays(-3),    directoryName, null, null, null, null, TestMarker + "4");

        workbook.SaveAs(path);
        return path;
    }

    [Fact]
    public async Task GercekDosyaVeGercekDb_AnalizVeImport_UctanUca()
    {
        await using var ctx = IntegrationDb.TryCreateContext()!;
        if (ctx is null) return; // DB erişilemiyor — test atlanır

        var counters       = await SnapshotCountersAsync(ctx);
        var directoryId    = Guid.NewGuid();
        var cargoCompanyId = Guid.NewGuid();
        // Ad benzersiz olmalı — dev DB'deki gerçek firmalarla çakışmasın
        var directoryName    = $"E2E Test Firması {directoryId:N}";
        var cargoCompanyName = $"E2E Kargo {cargoCompanyId:N}";
        string? filePath = null;

        try
        {
            // ── Hazırlık: rehber + kargo firması gerçek DB'ye eklenir ──
            ctx.Set<CompanyDirectory>().Add(new CompanyDirectory
            {
                Id = directoryId, CompanyName = directoryName,
                AddressLine = "E2E Test Adresi 1", City = "İstanbul", District = "Kadıköy",
                Phone = "0216 000 00 00", Email = "e2e@test.local",
                IsActive = true, CreatedAt = DateTime.UtcNow
            });
            ctx.Set<CargoCompany>().Add(new CargoCompany
            {
                Id = cargoCompanyId, Name = cargoCompanyName,
                IsActive = true, CreatedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            filePath = CreateSampleExcel(directoryName, cargoCompanyName);

            // ── Gerçek servis zinciri (DI ile aynı bileşenler) ──
            // Sprint 21: repository'ler IDbContextFactory alır (işlem başına taze context)
            var factory       = IntegrationDb.TryCreateFactory()!;
            var systemLog     = new NoOpSystemLogService();
            var shipmentRepo  = new CargoShipmentRepository(factory, systemLog);
            var directoryRepo = new CompanyDirectoryRepository(factory);
            var cargoRepo     = new CargoCompanyRepository(factory);
            var user          = new FakeUserContext();
            user.GrantAll();
            var audit = new FakeAuditLogService();
            var cache = new FakeCargoDashboardCache();

            var analyze = new AnalyzeCargoImportHandler(
                new ExcelCargoImportReader(), directoryRepo, cargoRepo, shipmentRepo, user, systemLog,
                new FakeTextNormalizationService());

            // ── 1) Analiz ──
            var analysis = await analyze.HandleAsync(new AnalyzeCargoImportRequest
            {
                FilePath = filePath, Direction = CargoShipmentDirection.Outgoing
            });

            Assert.True(analysis.Success, analysis.ErrorMessage ?? "");
            var a = analysis.Data!;
            Assert.Equal(6, a.Rows.Count);          // 7 fiziksel veri satırı (2-8) - 1 boş
            Assert.Equal(1, a.SkippedEmptyRows);
            Assert.Equal(4, a.ValidCount);
            Assert.Equal(1, a.ErrorCount);          // bilinmeyen firma
            Assert.Equal(1, a.DuplicateCount);      // aynı takip no (kesin)

            var duplicate = a.Rows.Single(r => r.Status == CargoImportRowStatus.Duplicate);
            Assert.Equal(DuplicateKind.TrackingNumberInFile, duplicate.DuplicateReason!.Kind);
            Assert.False(duplicate.CanInclude);

            // Firma çözümleme: büyük harfli yazım da aynı rehber kaydına bağlandı
            Assert.All(a.Rows.Where(r => r.Status == CargoImportRowStatus.Valid),
                r => Assert.Equal(directoryId, r.CompanyDirectoryId));

            // ── 2) Import (yalnızca geçerli satırlar — UI varsayılanıyla aynı) ──
            var approved = a.Rows.Where(r => r.IncludedByDefault).ToList();
            Assert.Equal(4, approved.Count);

            var import = new ImportCargoShipmentsHandler(
                shipmentRepo, directoryRepo, cargoRepo, audit, systemLog, cache, user,
                new FakeTextNormalizationService());

            var result = await import.HandleAsync(new ImportCargoShipmentsRequest
            {
                Direction              = CargoShipmentDirection.Outgoing,
                SourceName             = Path.GetFileName(filePath),
                Rows                   = approved,
                CreatedByUserId        = user.UserId,
                AnalysisTotalRows      = a.Rows.Count,
                AnalysisValidCount     = a.ValidCount,
                AnalysisWarningCount   = a.WarningCount,
                AnalysisErrorCount     = a.ErrorCount,
                AnalysisDuplicateCount = a.DuplicateCount
            });

            Assert.True(result.Success, result.ErrorMessage ?? "");
            Assert.Equal(4, result.Data!.ImportedCount);

            // ── 3) Gerçek DB doğrulaması ──
            var saved = await ctx.CargoShipments.AsNoTracking()
                .Where(s => s.Notes != null && s.Notes.StartsWith(TestMarker))
                .OrderBy(s => s.ShipmentNumber)
                .ToListAsync();

            Assert.Equal(4, saved.Count);
            Assert.All(saved, s =>
            {
                Assert.Equal(CargoShipmentCreatedFrom.ExcelImport, s.CreatedFrom);
                Assert.Equal(CargoShipmentStatus.Prepared, s.Status);
                Assert.Equal(directoryId, s.CompanyDirectoryId);
                Assert.StartsWith("GDN", s.ShipmentNumber);
                Assert.Equal(directoryName, s.ReceiverCompanyNameSnapshot);
            });

            // Numaralar ardışık ve sayaç tam +4 ilerledi
            var baseSeq = counters.GetValueOrDefault((int)CargoShipmentDirection.Outgoing);
            var current = await SnapshotCountersAsync(ctx);
            Assert.Equal(baseSeq + 4, current[(int)CargoShipmentDirection.Outgoing]);

            // Audit: 4 create + 1 özet
            Assert.Equal(4, audit.Entries.Count(e => e.Action == AuditAction.CargoShipmentCreated));
            Assert.Single(audit.Entries, e => e.Action == AuditAction.CargoImportCompleted);
            Assert.Equal(1, cache.InvalidateCount);

            // ── 4) Aynı dosya ikinci kez analiz edilirse: hepsi DB mükerreri olmalı ──
            // Taze factory ile ikinci analiz zinciri (production'da da her handler kendi context'ini alır)
            var factory2 = IntegrationDb.TryCreateFactory();
            if (factory2 is not null)
            {
                var analyze2 = new AnalyzeCargoImportHandler(
                    new ExcelCargoImportReader(),
                    new CompanyDirectoryRepository(factory2),
                    new CargoCompanyRepository(factory2),
                    new CargoShipmentRepository(factory2, systemLog),
                    user, systemLog, new FakeTextNormalizationService());

                var second = await analyze2.HandleAsync(new AnalyzeCargoImportRequest
                {
                    FilePath = filePath, Direction = CargoShipmentDirection.Outgoing
                });

                Assert.True(second.Success, second.ErrorMessage ?? "");
                // Daha önce içe aktarılan 4 satırın tamamı artık mükerrer olarak işaretlenmeli
                Assert.Equal(0, second.Data!.ValidCount + second.Data.WarningCount);
                Assert.True(second.Data.DuplicateCount >= 4);
            }
        }
        finally
        {
            if (filePath is not null)
                try { File.Delete(filePath); } catch { }
            await CleanupAsync(ctx, counters, directoryId, cargoCompanyId);
        }
    }
}
