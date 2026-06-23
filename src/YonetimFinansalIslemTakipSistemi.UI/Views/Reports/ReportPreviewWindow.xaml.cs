using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YonetimFinansalIslemTakipSistemi.Application.Features.Reports.Queries.GetReport;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Domain.Extensions;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Reports;

public partial class ReportPreviewWindow : Window
{
    private readonly ReportDto            _report;
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
        DateRangeText.Text   = FormatDateRange(_report);
        FilterSummaryText.Text = BuildFilterSummary(_report);
        FilterSummaryText.Visibility = string.IsNullOrEmpty(FilterSummaryText.Text)
            ? Visibility.Collapsed
            : Visibility.Visible;

        // Para birimi özet kartları — sadece filtreli para birimleri gösterilir
        SummaryGrid.Children.Clear();
        SummaryGrid.Columns = Math.Max(1, _report.CurrencySummaries.Count);
        foreach (var cs in _report.CurrencySummaries)
        {
            SummaryGrid.Children.Add(BuildCurrencyCard(cs));
        }

        // İşlem türü tablosu
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

            // Giriş = Alacak, Çıkış = Borç
            var isInflow = ts.TransactionType.GetFinancialDirection() == FinancialDirection.Inflow;
            return new TypeRow(
                ts.TypeDisplay,
                TryBorc:   isInflow ? 0m     : tryAmt,
                TryAlacak: isInflow ? tryAmt : 0m,
                TryCount:  tryCnt,
                UsdBorc:   isInflow ? 0m     : usdAmt,
                UsdAlacak: isInflow ? usdAmt : 0m,
                UsdCount:  usdCnt,
                EurBorc:   isInflow ? 0m     : eurAmt,
                EurAlacak: isInflow ? eurAmt : 0m,
                EurCount:  eurCnt);
        }).ToList();

        // Detay satırları
        if (_report.TransactionDetails is { Count: > 0 })
        {
            DetailSection.Visibility = Visibility.Visible;
            DetailDataGrid.ItemsSource = _report.TransactionDetails.Select(d => new DetailRow(
                d.TransactionDate.ToString("dd.MM.yyyy"),
                d.Description,
                d.TypeDisplay,
                d.CurrencyDisplay,
                d.Borc,
                d.Alacak,
                d.Balance)).ToList();
        }

        // Genel toplamlar paneli
        BuildTotalsPanel();
    }

    private void BuildTotalsPanel()
    {
        TotalsPanel.Child = null;
        var sp = new StackPanel { Orientation = Orientation.Vertical };

        var header = new TextBlock
        {
            Text       = "Genel Toplam",
            FontWeight = FontWeights.SemiBold,
            Margin     = new Thickness(0, 0, 0, 6)
        };
        sp.Children.Add(header);

        foreach (var cs in _report.CurrencySummaries)
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });

            AddTotalCell(grid, cs.CurrencyDisplay, 0, FontWeights.SemiBold, null);
            AddTotalCell(grid, $"Giriş: {cs.TotalInflow:N2}", 1, FontWeights.Normal, Brushes.DarkGreen);
            AddTotalCell(grid, $"Çıkış: {cs.TotalOutflow:N2}", 2, FontWeights.Normal, Brushes.DarkRed);
            AddTotalCell(grid, $"Net: {cs.NetBalance:N2}", 3, FontWeights.Bold, null);

            sp.Children.Add(grid);
        }

        TotalsPanel.Child = sp;
    }

    private static void AddTotalCell(Grid grid, string text, int col, FontWeight weight, Brush? foreground)
    {
        var tb = new TextBlock
        {
            Text         = text,
            FontWeight   = weight,
            Margin       = new Thickness(0, 0, 16, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        if (foreground is not null) tb.Foreground = foreground;
        Grid.SetColumn(tb, col);
        grid.Children.Add(tb);
    }

    private static GroupBox BuildCurrencyCard(CurrencySummaryDto cs)
    {
        var grid = new Grid { Margin = new Thickness(4) };
        for (var i = 0; i < 4; i++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        AddCardRow(grid, 0, "Toplam Giriş:", cs.TotalInflow.ToString("N2"), Brushes.DarkGreen);
        AddCardRow(grid, 1, "Toplam Çıkış:", cs.TotalOutflow.ToString("N2"), Brushes.DarkRed);

        var sep = new Separator { Margin = new Thickness(0, 4, 0, 4) };
        Grid.SetRow(sep, 2);
        Grid.SetColumnSpan(sep, 2);
        grid.Children.Add(sep);

        AddCardRow(grid, 3, "Net Bakiye:", cs.NetBalance.ToString("N2"), null, bold: true);

        return new GroupBox { Header = cs.CurrencyDisplay, Padding = new Thickness(8), Content = grid, Margin = new Thickness(0, 0, 6, 0) };
    }

    private static void AddCardRow(Grid grid, int row, string label, string value, Brush? valueBrush, bool bold = false)
    {
        var lblTb = new TextBlock { Text = label, Margin = new Thickness(0, 2, 0, 2), FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal };
        Grid.SetRow(lblTb, row); Grid.SetColumn(lblTb, 0);

        var valTb = new TextBlock
        {
            Text              = value,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin            = new Thickness(4, 2, 0, 2),
            FontWeight        = bold ? FontWeights.SemiBold : FontWeights.Normal
        };
        if (valueBrush is not null) valTb.Foreground = valueBrush;
        Grid.SetRow(valTb, row); Grid.SetColumn(valTb, 1);

        grid.Children.Add(lblTb);
        grid.Children.Add(valTb);
    }

    private static string BuildFilterSummary(ReportDto report)
    {
        var parts = new List<string>();
        if (report.FilterTransactionType.HasValue)
            parts.Add($"Tür: {(report.FilterTransactionType == TransactionType.Giris ? "Giriş" : "Çıkış")}");
        if (report.FilterCurrencyType.HasValue)
            parts.Add($"Para Birimi: {report.FilterCurrencyType}");
        if (!string.IsNullOrWhiteSpace(report.FilterDescription))
            parts.Add($"Açıklama: \"{report.FilterDescription}\"");
        return parts.Count == 0 ? string.Empty : "Filtre: " + string.Join(" | ", parts);
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

        var filePath    = dialog.FileName;
        string? partial = filePath;

        try
        {
            export(filePath);
            partial = null;
            _dialogService.ShowSuccess($"Rapor başarıyla kaydedildi.\n{filePath}");
        }
        catch (IOException ex) when ((ex.HResult & 0xFFFF) == 32)
        {
            _dialogService.ShowError("Dosya başka bir program tarafından kullanılıyor. Kapatıp tekrar deneyin.");
        }
        catch (Exception)
        {
            _dialogService.ShowError("Dosya kaydedilirken beklenmeyen bir hata oluştu.");
        }
        finally
        {
            if (partial is not null && File.Exists(partial))
                try { File.Delete(partial); } catch { }
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

    // Preview DataGrid için yerel kayıt tipleri
    private sealed record TypeRow(
        string  TypeDisplay,
        decimal TryBorc,   decimal TryAlacak,  int TryCount,
        decimal UsdBorc,   decimal UsdAlacak,  int UsdCount,
        decimal EurBorc,   decimal EurAlacak,  int EurCount);

    private sealed record DetailRow(
        string  Date,
        string  Description,
        string  TypeDisplay,
        string  CurrencyDisplay,
        decimal Borc,
        decimal Alacak,
        decimal Balance);
}
