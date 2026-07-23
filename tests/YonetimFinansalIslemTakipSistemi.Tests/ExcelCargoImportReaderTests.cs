using ClosedXML.Excel;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Excel okuyucu: gerçek ClosedXML ile yaz-oku turu (dosya fixture'ı gerekmez).
/// Tarih hücresi → dd.MM.yyyy, sayısal hücre → bilimsel gösterimsiz metin
/// dönüşümleri analiz katmanının sözleşmesidir.
/// </summary>
public class ExcelCargoImportReaderTests : IDisposable
{
    private readonly string _tempDir;

    public ExcelCargoImportReaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "yonetim-import-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private string TempFile(string name) => Path.Combine(_tempDir, name);

    [Fact]
    public async Task YazOkuTuru_TarihVeSayiHucreleri_BeklenenMetneDonusur()
    {
        var path = TempFile("ornek.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var ws = workbook.Worksheets.Add("Kargo");
            ws.Cell(1, 1).Value = "Tarih";
            ws.Cell(1, 2).Value = "Firma";
            ws.Cell(1, 3).Value = "Takip No";

            ws.Cell(2, 1).Value = new DateTime(2026, 6, 15);   // gerçek tarih hücresi
            ws.Cell(2, 2).Value = "Acme A.Ş.";
            ws.Cell(2, 3).Value = 123456789012;                 // sayısal takip no

            ws.Cell(3, 1).Value = "16.06.2026";                 // metin tarih
            ws.Cell(3, 2).Value = "  İnci  Ticaret ";
            // 3. hücre boş
            workbook.SaveAs(path);
        }

        var document = await new ExcelCargoImportReader().ReadAsync(path);

        Assert.Equal("ornek.xlsx", document.SourceName);
        Assert.Equal(["Tarih", "Firma", "Takip No"], document.Headers);
        Assert.Equal(2, document.Rows.Count);

        var first = document.Rows[0];
        Assert.Equal(2, first.RowNumber);
        Assert.Equal("15.06.2026", first.Cells[0]);        // tarih hücresi normalize
        Assert.Equal("123456789012", first.Cells[2]);      // bilimsel gösterim yok

        var second = document.Rows[1];
        Assert.Equal("16.06.2026", second.Cells[0]);
        Assert.Null(second.Cells[2]);                      // boş hücre null
        Assert.False(first.IsEmpty);
    }

    [Fact]
    public async Task XlsUzantisi_AnlasilirMesajlaReddedilir()
    {
        var path = TempFile("eski.xls");
        await File.WriteAllBytesAsync(path, [0x00]);

        var ex = await Assert.ThrowsAsync<ImportFileException>(
            () => new ExcelCargoImportReader().ReadAsync(path));

        Assert.Contains("xlsx", ex.Message);
    }

    [Fact]
    public async Task OlmayanDosya_Reddedilir()
    {
        var ex = await Assert.ThrowsAsync<ImportFileException>(
            () => new ExcelCargoImportReader().ReadAsync(TempFile("yok.xlsx")));

        Assert.Contains("bulunamadı", ex.Message);
    }

    [Fact]
    public async Task BozukDosya_AnlasilirMesajlaReddedilir()
    {
        var path = TempFile("bozuk.xlsx");
        await File.WriteAllTextAsync(path, "bu bir excel dosyası değil");

        var ex = await Assert.ThrowsAsync<ImportFileException>(
            () => new ExcelCargoImportReader().ReadAsync(path));

        Assert.Contains("açılamadı", ex.Message);
    }

    [Fact]
    public async Task Sablon_UretilirVeOkunur_TumZorunluKolonlarEslesir()
    {
        var path = TempFile("sablon.xlsx");
        new ExcelCargoImportTemplateService().CreateTemplate(path);

        var document = await new ExcelCargoImportReader().ReadAsync(path);
        var match    = CargoImportColumnMap.MatchHeaders(document.Headers);

        Assert.Empty(match.MissingRequired);
        Assert.Empty(match.ExtraHeaders);
        Assert.Equal(CargoImportColumnMap.Columns.Count, match.Indexes.Count);
    }
}
