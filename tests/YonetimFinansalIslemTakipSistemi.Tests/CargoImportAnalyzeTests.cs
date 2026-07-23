using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Analiz handler'ı: kolon/satır doğrulama, firma çözümleme, mükerrer tespiti.
/// Okuyucu sahte olduğundan testler dosya formatından tamamen bağımsızdır —
/// mimarinin format bağımsızlık hedefinin doğrulaması da budur.
/// </summary>
public class CargoImportAnalyzeTests
{
    private sealed class FakeImportFileReader : ICargoImportFileReader
    {
        public ImportDocument? Document { get; set; }
        public ImportFileException? ThrowOnRead { get; set; }

        public Task<ImportDocument> ReadAsync(string filePath)
            => ThrowOnRead is not null ? throw ThrowOnRead : Task.FromResult(Document!);
    }

    private static readonly string[] DefaultHeaders =
        ["Tarih", "Firma", "Kargo Firması", "Öncelik", "Takip No"];

    private static ImportDocument Doc(params string?[][] rows) => new()
    {
        SourceName = "test.xlsx",
        Headers    = DefaultHeaders,
        Rows       = rows.Select((cells, i) => new ImportDocumentRow { RowNumber = i + 2, Cells = cells }).ToList()
    };

    private sealed record Env(
        AnalyzeCargoImportHandler Handler,
        FakeImportFileReader Reader,
        FakeCompanyDirectoryRepository Directories,
        FakeCargoCompanyRepository CargoCompanies,
        FakeCargoShipmentRepository Shipments,
        Guid AcmeId);

    private static Env Build(bool grantPermission = true)
    {
        var reader      = new FakeImportFileReader();
        var directories = new FakeCompanyDirectoryRepository();
        var cargoRepo   = new FakeCargoCompanyRepository();
        var shipments   = new FakeCargoShipmentRepository();
        var user        = new FakeUserContext();
        if (grantPermission) user.GrantAll();

        var acmeId = Guid.NewGuid();
        directories.Items.Add(new CompanyDirectory
        {
            Id = acmeId, CompanyName = "Acme A.Ş.", AddressLine = "Sanayi Cad. 5",
            City = "İstanbul", District = "Kadıköy", Phone = "0216 111 11 11",
            Email = "info@acme.com", AttentionTo = "Satın Alma", IsActive = true
        });
        cargoRepo.Items.Add(new CargoCompany { Id = Guid.NewGuid(), Name = "Hızlı Kargo", IsActive = true });

        var handler = new AnalyzeCargoImportHandler(
            reader, directories, cargoRepo, shipments, user, new FakeSystemLogService());

        return new Env(handler, reader, directories, cargoRepo, shipments, acmeId);
    }

    private static AnalyzeCargoImportRequest Request() => new()
    {
        FilePath  = "test.xlsx",
        Direction = CargoShipmentDirection.Outgoing
    };

    [Fact]
    public async Task YetkisizKullanici_Reddedilir()
    {
        var env = Build(grantPermission: false);

        var result = await env.Handler.HandleAsync(Request());

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GecerliSatir_FirmaCozumlenir_SnapshotDolar()
    {
        var env = Build();
        env.Reader.Document = Doc(["15.06.2026", "acme a.ş.", "Hızlı Kargo", "Acil", "TRK123"]);

        var result = await env.Handler.HandleAsync(Request());

        Assert.True(result.Success);
        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal(CargoImportRowStatus.Valid, row.Status);
        Assert.Equal(env.AcmeId, row.CompanyDirectoryId);
        Assert.Equal("Acme A.Ş.", row.ReceiverCompanyNameSnapshot);
        Assert.Equal("Sanayi Cad. 5", row.ReceiverAddressSnapshot);
        Assert.Equal(CargoShipmentPriority.Urgent, row.Priority);
        Assert.NotNull(row.CargoCompanyId);
    }

    [Fact]
    public async Task EksikZorunluKolon_AnalizBastanReddedilir()
    {
        var env = Build();
        env.Reader.Document = new ImportDocument
        {
            SourceName = "test.xlsx",
            Headers    = ["Firma", "Takip No"], // Tarih yok
            Rows       = []
        };

        var result = await env.Handler.HandleAsync(Request());

        Assert.False(result.Success);
        Assert.Contains("Tarih", result.ErrorMessage);
    }

    [Fact]
    public async Task DosyaHatasi_KullaniciyaMesajiylaDoner()
    {
        var env = Build();
        env.Reader.ThrowOnRead = new ImportFileException("Yalnızca .xlsx uzantılı Excel dosyaları desteklenir.");

        var result = await env.Handler.HandleAsync(Request());

        Assert.False(result.Success);
        Assert.Contains(".xlsx", result.ErrorMessage);
    }

    [Fact]
    public async Task BosSatirlar_SessizceAtlanirVeSayilir()
    {
        var env = Build();
        env.Reader.Document = Doc(
            ["15.06.2026", "Acme A.Ş.", null, null, null],
            [null, null, null, null, null],
            ["", "  ", null, null, null]);

        var result = await env.Handler.HandleAsync(Request());

        Assert.True(result.Success);
        Assert.Single(result.Data!.Rows);
        Assert.Equal(2, result.Data.SkippedEmptyRows);
    }

    [Fact]
    public async Task BulunamayanFirma_HataVeOneriIcerir()
    {
        var env = Build();
        env.Reader.Document = Doc(["15.06.2026", "Acme", null, null, null]);

        var result = await env.Handler.HandleAsync(Request());

        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal(CargoImportRowStatus.Error, row.Status);
        Assert.Contains(row.Messages, m => m.Message.Contains("Acme A.Ş."));  // öneri
        Assert.False(row.CanInclude);
    }

    [Fact]
    public async Task GecersizTarih_Hata()
    {
        var env = Build();
        env.Reader.Document = Doc(["yarın", "Acme A.Ş.", null, null, null]);

        var result = await env.Handler.HandleAsync(Request());

        Assert.Equal(CargoImportRowStatus.Error, Assert.Single(result.Data!.Rows).Status);
    }

    [Fact]
    public async Task TaninmayanOncelik_UyariVeNormalKabul()
    {
        var env = Build();
        env.Reader.Document = Doc(["15.06.2026", "Acme A.Ş.", null, "Süper Acil", null]);

        var result = await env.Handler.HandleAsync(Request());

        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal(CargoImportRowStatus.Warning, row.Status);
        Assert.Equal(CargoShipmentPriority.Normal, row.Priority);
        Assert.True(row.CanInclude);
    }

    [Fact]
    public async Task DosyaIciAyniTakipNo_KesinMukerrer_DahilEdilemez()
    {
        var env = Build();
        env.Reader.Document = Doc(
            ["15.06.2026", "Acme A.Ş.", null, null, "TRK1"],
            ["16.06.2026", "Acme A.Ş.", null, null, "trk1 "]); // normalize eşleşir

        var result = await env.Handler.HandleAsync(Request());

        var rows = result.Data!.Rows;
        Assert.Equal(CargoImportRowStatus.Valid, rows[0].Status);
        Assert.Equal(CargoImportRowStatus.Duplicate, rows[1].Status);
        var reason = rows[1].DuplicateReason!;
        Assert.Equal(DuplicateKind.TrackingNumberInFile, reason.Kind);
        Assert.True(reason.IsExact);
        Assert.False(rows[1].CanInclude);
        Assert.Equal(rows[0].RowNumber, reason.MatchedRowNumber);
    }

    [Fact]
    public async Task VeritabanindaAyniTakipNo_KesinMukerrer()
    {
        var env = Build();
        env.Shipments.Existing.Add(new CargoShipment
        {
            Id = Guid.NewGuid(), Direction = CargoShipmentDirection.Outgoing,
            ShipmentNumber = "GDN00042", TrackingNumber = "TRK9",
            ShipmentDate = DateTime.SpecifyKind(new DateTime(2026, 6, 1), DateTimeKind.Utc)
        });
        env.Reader.Document = Doc(["15.06.2026", "Acme A.Ş.", null, null, "TRK9"]);

        var result = await env.Handler.HandleAsync(Request());

        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal(DuplicateKind.TrackingNumberInDatabase, row.DuplicateReason!.Kind);
        Assert.Equal("GDN00042", row.DuplicateReason.MatchedShipmentNumber);
        Assert.False(row.CanInclude);
    }

    [Fact]
    public async Task VeritabanindaBenzerKayit_OlasiMukerrer_DahilEdilebilir()
    {
        var env = Build();
        env.Shipments.Existing.Add(new CargoShipment
        {
            Id = Guid.NewGuid(), Direction = CargoShipmentDirection.Outgoing,
            ShipmentNumber = "GDN00007",
            CompanyDirectoryId = env.AcmeId,
            ShipmentDate = DateTime.SpecifyKind(new DateTime(2026, 6, 15), DateTimeKind.Utc)
        });
        env.Reader.Document = Doc(["15.06.2026", "Acme A.Ş.", null, null, null]);

        var result = await env.Handler.HandleAsync(Request());

        var row = Assert.Single(result.Data!.Rows);
        Assert.Equal(CargoImportRowStatus.Duplicate, row.Status);
        Assert.Equal(DuplicateKind.SimilarInDatabase, row.DuplicateReason!.Kind);
        Assert.False(row.DuplicateReason.IsExact);
        Assert.True(row.CanInclude);          // kullanıcı bilinçli dahil edebilir
        Assert.False(row.IncludedByDefault);  // varsayılan hariç
    }
}
