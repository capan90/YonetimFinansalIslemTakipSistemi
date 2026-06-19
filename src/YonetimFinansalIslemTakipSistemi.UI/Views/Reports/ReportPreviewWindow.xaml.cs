using Microsoft.Win32;
using System.IO;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.Reports.Queries.GetReport;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Reports;

public partial class ReportPreviewWindow : Window
{
    private readonly ReportDto           _report;
    private readonly IReportExportService _exportService;
    private readonly IDialogService       _dialogService;

    public ReportPreviewWindow(
        ReportDto            report,
        IReportExportService exportService,
        IDialogService       dialogService)
    {
        InitializeComponent();
        _report        = report;
        _exportService = exportService;
        _dialogService = dialogService;

        PopulateView();
    }

    private void PopulateView()
    {
        DateRangeText.Text = FormatDateRange(_report);

        var tl  = _report.CurrencySummaries.FirstOrDefault(c => c.Currency == CurrencyType.TRY);
        var usd = _report.CurrencySummaries.FirstOrDefault(c => c.Currency == CurrencyType.USD);
        var eur = _report.CurrencySummaries.FirstOrDefault(c => c.Currency == CurrencyType.EUR);

        TlInflowText.Text   = (tl?.TotalInflow   ?? 0).ToString("N2");
        TlOutflowText.Text  = (tl?.TotalOutflow  ?? 0).ToString("N2");
        TlNetText.Text      = (tl?.NetBalance    ?? 0).ToString("N2");
        UsdInflowText.Text  = (usd?.TotalInflow  ?? 0).ToString("N2");
        UsdOutflowText.Text = (usd?.TotalOutflow ?? 0).ToString("N2");
        UsdNetText.Text     = (usd?.NetBalance   ?? 0).ToString("N2");
        EurInflowText.Text  = (eur?.TotalInflow  ?? 0).ToString("N2");
        EurOutflowText.Text = (eur?.TotalOutflow ?? 0).ToString("N2");
        EurNetText.Text     = (eur?.NetBalance   ?? 0).ToString("N2");

        TypeDataGrid.ItemsSource = _report.TransactionTypeSummaries.Select(ts =>
        {
            decimal tryAmt = 0, usdAmt = 0, eurAmt = 0;
            int tryCnt = 0, usdCnt = 0, eurCnt = 0;

            foreach (var ca in ts.AmountsByCurrency)
            {
                switch (ca.Currency)
                {
                    case CurrencyType.TRY: tryAmt = ca.TotalAmount; tryCnt = ca.Count; break;
                    case CurrencyType.USD: usdAmt = ca.TotalAmount; usdCnt = ca.Count; break;
                    case CurrencyType.EUR: eurAmt = ca.TotalAmount; eurCnt = ca.Count; break;
                }
            }

            return new TypeRow(ts.TypeDisplay, tryAmt, tryCnt, usdAmt, usdCnt, eurAmt, eurCnt);
        }).ToList();
    }

    private void SavePdf_Click(object sender, RoutedEventArgs e)
        => RunExport(
            filter:          "PDF Dosyası|*.pdf",
            defaultFileName: $"rapor_{FilenameSuffix(_report)}.pdf",
            export:          path => _exportService.ExportToPdf(_report, path));

    private void SaveExcel_Click(object sender, RoutedEventArgs e)
        => RunExport(
            filter:          "Excel Dosyası|*.xlsx",
            defaultFileName: $"rapor_{FilenameSuffix(_report)}.xlsx",
            export:          path => _exportService.ExportToExcel(_report, path));

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void RunExport(string filter, string defaultFileName, Action<string> export)
    {
        var dialog = new SaveFileDialog
        {
            Title      = "Raporu Kaydet",
            Filter     = filter,
            FileName   = defaultFileName,
            DefaultExt = Path.GetExtension(defaultFileName).TrimStart('.')
        };

        if (dialog.ShowDialog() != true) return;

        var filePath = dialog.FileName;
        // Kısmen yazılan dosyayı temizlemek için tutulur; başarılı olunca null yapılır
        string? partialFile = filePath;

        try
        {
            export(filePath);
            partialFile = null;
            _dialogService.ShowSuccess($"Rapor başarıyla kaydedildi.\n{filePath}");
        }
        catch (IOException ex) when ((ex.HResult & 0xFFFF) == 32) // ERROR_SHARING_VIOLATION
        {
            _dialogService.ShowError("Dosya başka bir program tarafından kullanılıyor. Kapatıp tekrar deneyin.");
        }
        catch (Exception)
        {
            _dialogService.ShowError("Dosya kaydedilirken beklenmeyen bir hata oluştu.");
        }
        finally
        {
            if (partialFile is not null && File.Exists(partialFile))
            {
                try { File.Delete(partialFile); }
                catch { /* silme denemesi başarısız olsa da kullanıcıya hata mesajı zaten gösterildi */ }
            }
        }
    }

    private static string FormatDateRange(ReportDto report)
    {
        if (report.StartDate.HasValue && report.EndDate.HasValue)
            return $"{report.StartDate:dd.MM.yyyy} – {report.EndDate:dd.MM.yyyy}";
        if (report.StartDate.HasValue)
            return $"{report.StartDate:dd.MM.yyyy} tarihinden itibaren";
        if (report.EndDate.HasValue)
            return $"{report.EndDate:dd.MM.yyyy} tarihine kadar";
        return "Tüm zamanlar";
    }

    private static string FilenameSuffix(ReportDto report)
    {
        if (report.StartDate.HasValue && report.EndDate.HasValue)
            return $"{report.StartDate:yyyy-MM-dd}_{report.EndDate:yyyy-MM-dd}";
        return "tum_zamanlar";
    }

    // DataGrid bağlaması için önizleme penceresi yerel read modeli
    private sealed record TypeRow(
        string  TypeDisplay,
        decimal TryAmount, int TryCount,
        decimal UsdAmount, int UsdCount,
        decimal EurAmount, int EurCount);
}
