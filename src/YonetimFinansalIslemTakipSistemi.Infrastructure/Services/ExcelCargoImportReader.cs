using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// ICargoImportFileReader'ın Excel (xlsx) implementasyonu — ClosedXML tabanlı,
/// ReportExportService'in okuma simetriği. UI ve Application katmanları ClosedXML'i
/// bilmez; yalnızca format bağımsız ImportDocument görür.
/// xls (eski format) bilinçli olarak desteklenmez: ClosedXML okuyamaz ve ikinci bir
/// kütüphane eklemek kalıcı bağımlılık maliyetidir — kullanıcı dosyayı xlsx kaydeder.
/// </summary>
public class ExcelCargoImportReader : ICargoImportFileReader
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    // 3000: mevcut rehber taşıma dosyası ~2.1K satır — tek seferde içe aktarılabilsin
    public const int  MaxDataRows      = 3000;

    public Task<ImportDocument> ReadAsync(string filePath)
        // ClosedXML senkron çalışır; UI thread'ini bloklamamak için arka plana alınır
        => Task.Run(() => Read(filePath));

    private static ImportDocument Read(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension == ".xls")
            throw new ImportFileException(
                "Eski Excel biçimi (.xls) desteklenmiyor. Dosyayı Excel'de açıp " +
                "\"Farklı Kaydet → Excel Çalışma Kitabı (.xlsx)\" ile kaydedin ve tekrar deneyin.");
        if (extension != ".xlsx")
            throw new ImportFileException("Yalnızca .xlsx uzantılı Excel dosyaları desteklenir.");

        var info = new FileInfo(filePath);
        if (!info.Exists)
            throw new ImportFileException("Dosya bulunamadı veya erişilemiyor.");
        if (info.Length > MaxFileSizeBytes)
            throw new ImportFileException(
                $"Dosya çok büyük ({info.Length / (1024 * 1024)} MB). Üst sınır: {MaxFileSizeBytes / (1024 * 1024)} MB.");

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(filePath);
        }
        catch (Exception ex)
        {
            throw new ImportFileException(
                "Dosya açılamadı — bozuk veya geçerli bir Excel dosyası değil.", ex);
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new ImportFileException("Dosyada çalışma sayfası bulunamadı.");

            var firstRow = worksheet.FirstRowUsed();
            var lastRow  = worksheet.LastRowUsed();
            if (firstRow is null || lastRow is null)
                throw new ImportFileException("Dosya boş — başlık satırı bulunamadı.");

            var lastColumn = worksheet.LastColumnUsed()!.ColumnNumber();
            var headerRowNumber = firstRow.RowNumber();

            var headers = new List<string>(lastColumn);
            for (var c = 1; c <= lastColumn; c++)
                headers.Add(CellText(worksheet.Cell(headerRowNumber, c)) ?? string.Empty);

            var dataRowCount = lastRow.RowNumber() - headerRowNumber;
            if (dataRowCount > MaxDataRows)
                throw new ImportFileException(
                    $"Dosyada {dataRowCount} veri satırı var. Üst sınır: {MaxDataRows}. " +
                    "Dosyayı bölerek birden fazla seferde içe aktarın.");

            var rows = new List<ImportDocumentRow>(Math.Max(0, dataRowCount));
            for (var r = headerRowNumber + 1; r <= lastRow.RowNumber(); r++)
            {
                var cells = new List<string?>(lastColumn);
                for (var c = 1; c <= lastColumn; c++)
                    cells.Add(CellText(worksheet.Cell(r, c)));

                rows.Add(new ImportDocumentRow { RowNumber = r, Cells = cells });
            }

            return new ImportDocument
            {
                SourceName = Path.GetFileName(filePath),
                Headers    = headers,
                Rows       = rows
            };
        }
    }

    /// <summary>
    /// Hücreyi analiz katmanının beklediği metne çevirir:
    /// tarih → dd.MM.yyyy, saat → HH:mm, sayı → invariant (takip numaraları
    /// sayısal hücreye yazılmış olabilir; bilimsel gösterim/binlik ayracı istenmez).
    /// </summary>
    private static string? CellText(IXLCell cell)
    {
        var value = cell.Value;

        if (value.IsBlank) return null;
        if (value.IsDateTime) return value.GetDateTime().ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        if (value.IsTimeSpan) return value.GetTimeSpan().ToString(@"hh\:mm", CultureInfo.InvariantCulture);
        if (value.IsBoolean) return value.GetBoolean() ? "Evet" : "Hayır";
        if (value.IsNumber)
        {
            var number = value.GetNumber();
            return number == Math.Truncate(number) && Math.Abs(number) < 1e15
                ? ((long)number).ToString(CultureInfo.InvariantCulture)
                : number.ToString(CultureInfo.InvariantCulture);
        }

        var text = value.IsText ? value.GetText() : cell.GetFormattedString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}

/// <summary>
/// Kullanıcının dolduracağı boş xlsx şablonlarını üretir — başlıklar ilgili
/// kolon şemalarından gelir, böylece her şema tek kaynaktan yönetilir.
/// </summary>
public class ExcelCargoImportTemplateService : ICargoImportTemplateService
{
    public void CreateTemplate(string filePath)
        => Write(filePath, "Kargo",
            CargoImportColumnMap.Columns.Select(c => (c.Header, c.Required)).ToList(),
            [DateTime.Today.ToString("dd.MM.yyyy"), "Örnek Firma A.Ş."]);

    public void CreateDirectoryTemplate(string filePath)
        => Write(filePath, "Firma Rehberi",
            Application.Features.CompanyDirectory.Import.DirectoryImportColumnMap.Columns
                .Select(c => (c.Header, c.Required)).ToList(),
            ["Örnek Firma A.Ş.", "Ali Veli", "", "Sanayi Cad. No:5", "Kadıköy", "İstanbul", "", "0216 123 45 67"]);

    public void CreateWhatsAppTemplate(string filePath)
        => Write(filePath, "WhatsApp Rehberi",
            Application.Features.WhatsAppContacts.Import.WhatsAppImportColumnMap.Columns
                .Select(c => (c.Header, c.Required)).ToList(),
            ["Ali Veli", "0532 123 45 67", "Örnek Firma A.Ş."]);

    public void CreateCashTemplate(string filePath)
        => Write(filePath, "Finans",
            Application.Features.CashTransactions.Import.CashImportColumnMap.Columns
                .Select(c => (c.Header, c.Required)).ToList(),
            [DateTime.Today.ToString("dd.MM.yyyy"), "Market alışverişi", "", "150", "TL", "M. Yasav"]);

    private static void Write(string filePath, string sheetName,
        IReadOnlyList<(string Header, bool Required)> columns, IReadOnlyList<string> exampleRow)
    {
        using var workbook  = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        for (var i = 0; i < columns.Count; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = columns[i].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(columns[i].Required ? "#DCE6F1" : "#F2F2F2");
            worksheet.Column(i + 1).Width = Math.Max(14, columns[i].Header.Length + 6);
        }

        // Örnek biçim ipuçları — kullanıcı ilk satırı silip verisini yazar
        for (var i = 0; i < exampleRow.Count && i < columns.Count; i++)
            if (exampleRow[i].Length > 0)
                worksheet.Cell(2, i + 1).Value = exampleRow[i];
        worksheet.Row(2).Style.Font.FontColor = XLColor.Gray;

        worksheet.SheetView.FreezeRows(1);
        workbook.SaveAs(filePath);
    }
}
