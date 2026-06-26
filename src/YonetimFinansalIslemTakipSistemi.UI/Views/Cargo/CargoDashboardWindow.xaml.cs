using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Queries.GetCargoCompanyList;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoDashboard;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoReport;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

public partial class CargoDashboardWindow : Window
{
    private readonly IServiceProvider     _services;
    private readonly IDialogService       _dialogService;
    private CargoReportDto?               _lastReport;

    // ComboBox item sarmalayıcıları
    private record ComboItem<T>(string Label, T? Value);

    public CargoDashboardWindow(IServiceProvider services)
    {
        InitializeComponent();
        _services      = services;
        _dialogService = services.GetRequiredService<IDialogService>();

        Loaded += async (_, _) =>
        {
            try
            {
                PopulateFilterCombos();
                await LoadCargoCompaniesAsync();
                await LoadDashboardAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Dashboard yüklenirken hata oluştu:\n{ex.Message}", "Dashboard Hatası");
            }
        };
    }

    // ── Dashboard ──────────────────────────────────────────────────────────

    private async Task LoadDashboardAsync()
    {
        var handler = _services.GetRequiredService<GetCargoDashboardHandler>();
        var result  = await handler.HandleAsync(new GetCargoDashboardQuery
        {
            ChartDateFrom = DateTime.Today.AddDays(-30),
            ChartDateTo   = DateTime.Today,
        });

        if (!result.Success)
        {
            _dialogService.ShowError(result.ErrorMessage ?? "Dashboard yüklenemedi.", "Dashboard");
            return;
        }

        var dto = result.Data!;

        DashboardDateBlock.Text = $"Güncellendi: {DateTime.Now:dd.MM.yyyy HH:mm}";

        // Kartlar
        CardTodayIncomingVal.Text  = dto.TodayIncoming.ToString();
        CardTodayOutgoingVal.Text  = dto.TodayOutgoing.ToString();
        CardPendingVal.Text        = dto.Pending.ToString();
        CardNotifPendingVal.Text   = dto.NotificationPending.ToString();
        CardUrgentVal.Text         = dto.UrgentPending.ToString();
        CardTodayDeliveredVal.Text = dto.TodayDelivered.ToString();

        // Grafikler
        DirectionChart.ItemsSource = BuildChartItems(dto.DirectionChart);
        StatusChart.ItemsSource    = BuildChartItems(dto.StatusChart);
        CompanyChart.ItemsSource   = BuildChartItems(dto.CompanyChart);

        // Son 10 hareket
        RecentGrid.ItemsSource = dto.RecentShipments;
    }

    private async void RefreshDashboardButton_Click(object sender, RoutedEventArgs e)
    {
        // "Yenile" butonu cache'i atlayıp her zaman DB'den taze veri çeker
        var handler = _services.GetRequiredService<GetCargoDashboardHandler>();
        var result  = await handler.HandleAsync(new GetCargoDashboardQuery
        {
            ChartDateFrom = DateTime.Today.AddDays(-30),
            ChartDateTo   = DateTime.Today,
            BypassCache   = true,
        });

        if (!result.Success)
        {
            _dialogService.ShowError(result.ErrorMessage ?? "Dashboard yüklenemedi.", "Dashboard");
            return;
        }

        var dto = result.Data!;
        DashboardDateBlock.Text = $"Güncellendi: {DateTime.Now:dd.MM.yyyy HH:mm}";
        CardTodayIncomingVal.Text  = dto.TodayIncoming.ToString();
        CardTodayOutgoingVal.Text  = dto.TodayOutgoing.ToString();
        CardPendingVal.Text        = dto.Pending.ToString();
        CardNotifPendingVal.Text   = dto.NotificationPending.ToString();
        CardUrgentVal.Text         = dto.UrgentPending.ToString();
        CardTodayDeliveredVal.Text = dto.TodayDelivered.ToString();
        DirectionChart.ItemsSource = BuildChartItems(dto.DirectionChart);
        StatusChart.ItemsSource    = BuildChartItems(dto.StatusChart);
        CompanyChart.ItemsSource   = BuildChartItems(dto.CompanyChart);
        RecentGrid.ItemsSource     = dto.RecentShipments;
    }

    // ── Grafik yardımcı ───────────────────────────────────────────────────

    /// <summary>Bar genişliğini maksimum değere göre normalize eder (0–200 px arası).</summary>
    private static IReadOnlyList<ChartBarItem> BuildChartItems(IReadOnlyList<CargoDashboardChartItem> items)
    {
        if (items.Count == 0) return [];
        int maxVal = items.Max(i => i.Value);
        return items.Select(i =>
        {
            // Hex string → SolidColorBrush (WPF binding Background için doğrudan Brush gerekir)
            var brush = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(i.Color));
            return new ChartBarItem(
                i.Label,
                i.Value,
                brush,
                maxVal > 0 ? (int)(i.Value * 200.0 / maxVal) : 0);
        }).ToList();
    }

    // ── Filtre Combobox Doldurma ──────────────────────────────────────────

    private void PopulateFilterCombos()
    {
        // Yön
        DirectionCombo.ItemsSource = new[]
        {
            new ComboItem<CargoShipmentDirection?>("Tümü",  null),
            new ComboItem<CargoShipmentDirection?>("Gelen", CargoShipmentDirection.Incoming),
            new ComboItem<CargoShipmentDirection?>("Giden", CargoShipmentDirection.Outgoing),
        };
        DirectionCombo.DisplayMemberPath = "Label";
        DirectionCombo.SelectedIndex     = 0;

        // Durum
        StatusCombo.ItemsSource = new[]
        {
            new ComboItem<CargoShipmentStatus?>("Tümü",          null),
            new ComboItem<CargoShipmentStatus?>("Taslak",        CargoShipmentStatus.Draft),
            new ComboItem<CargoShipmentStatus?>("Hazırlandı",    CargoShipmentStatus.Prepared),
            new ComboItem<CargoShipmentStatus?>("Gönderildi",    CargoShipmentStatus.Shipped),
            new ComboItem<CargoShipmentStatus?>("Alındı",        CargoShipmentStatus.Received),
            new ComboItem<CargoShipmentStatus?>("Teslim Edildi", CargoShipmentStatus.Delivered),
            new ComboItem<CargoShipmentStatus?>("İptal",         CargoShipmentStatus.Cancelled),
        };
        StatusCombo.DisplayMemberPath = "Label";
        StatusCombo.SelectedIndex     = 0;

        // Bildirim Durumu
        NotifStatusCombo.ItemsSource = new[]
        {
            new ComboItem<CargoNotificationStatus?>("Tümü",           null),
            new ComboItem<CargoNotificationStatus?>("Bildirilmedi",   CargoNotificationStatus.NotNotified),
            new ComboItem<CargoNotificationStatus?>("WhatsApp Hazır", CargoNotificationStatus.WhatsAppPrepared),
            new ComboItem<CargoNotificationStatus?>("Mail Hazır",     CargoNotificationStatus.MailPrepared),
            new ComboItem<CargoNotificationStatus?>("Bildirildi",     CargoNotificationStatus.Notified),
        };
        NotifStatusCombo.DisplayMemberPath = "Label";
        NotifStatusCombo.SelectedIndex     = 0;

        // Öncelik
        PriorityCombo.ItemsSource = new[]
        {
            new ComboItem<CargoShipmentPriority?>("Tümü",     null),
            new ComboItem<CargoShipmentPriority?>("Normal",   CargoShipmentPriority.Normal),
            new ComboItem<CargoShipmentPriority?>("Orta",     CargoShipmentPriority.Medium),
            new ComboItem<CargoShipmentPriority?>("Acil",     CargoShipmentPriority.Urgent),
            new ComboItem<CargoShipmentPriority?>("Çok Acil", CargoShipmentPriority.Critical),
        };
        PriorityCombo.DisplayMemberPath = "Label";
        PriorityCombo.SelectedIndex     = 0;
    }

    private async Task LoadCargoCompaniesAsync()
    {
        var handler = _services.GetRequiredService<GetCargoCompanyListHandler>();
        var result  = await handler.HandleAsync(new GetCargoCompanyListQuery { IsActive = true });

        var items = new List<ComboItem<Guid?>> { new("Tümü", null) };
        if (result != null)
            items.AddRange(result.Select(c => new ComboItem<Guid?>(c.Name, c.Id)));

        CargoCompanyCombo.ItemsSource        = items;
        CargoCompanyCombo.DisplayMemberPath  = "Label";
        CargoCompanyCombo.SelectedIndex      = 0;
    }

    // ── Rapor ─────────────────────────────────────────────────────────────

    private async void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RunReportAsync();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Rapor alınırken hata oluştu:\n{ex.Message}", "Rapor Hatası");
        }
    }

    private async Task RunReportAsync()
    {
        ExportResultBorder.Visibility = Visibility.Collapsed;

        var companyItem = CargoCompanyCombo.SelectedItem as ComboItem<Guid?>;
        var query = new GetCargoReportQuery
        {
            DateFrom           = DateFromPicker.SelectedDate,
            DateTo             = DateToPicker.SelectedDate,
            Direction          = (DirectionCombo.SelectedItem as ComboItem<CargoShipmentDirection?>)?.Value,
            Keyword            = string.IsNullOrWhiteSpace(KeywordBox.Text) ? null : KeywordBox.Text.Trim(),
            CargoCompanyId     = companyItem?.Value,
            CargoCompanyName   = companyItem?.Value.HasValue == true ? companyItem.Label : null,
            Status             = (StatusCombo.SelectedItem as ComboItem<CargoShipmentStatus?>)?.Value,
            NotificationStatus = (NotifStatusCombo.SelectedItem as ComboItem<CargoNotificationStatus?>)?.Value,
            Priority           = (PriorityCombo.SelectedItem as ComboItem<CargoShipmentPriority?>)?.Value,
            VehiclePlate       = string.IsNullOrWhiteSpace(VehiclePlateBox.Text) ? null : VehiclePlateBox.Text.Trim(),
            TrackingNumber     = string.IsNullOrWhiteSpace(TrackingBox.Text) ? null : TrackingBox.Text.Trim(),
            ShipmentNumber     = string.IsNullOrWhiteSpace(ShipmentNoBox.Text) ? null : ShipmentNoBox.Text.Trim(),
        };

        var handler = _services.GetRequiredService<GetCargoReportHandler>();
        var result  = await handler.HandleAsync(query);

        if (!result.Success)
        {
            _dialogService.ShowError(result.ErrorMessage ?? "Rapor alınamadı.", "Rapor");
            return;
        }

        _lastReport = result.Data!;
        ReportGrid.ItemsSource = _lastReport.Rows;

        // Özet banner
        ReportSummaryBlock.Text =
            $"Toplam: {_lastReport.TotalCount} kayıt  |  " +
            $"Gelen: {_lastReport.IncomingCount}  |  Giden: {_lastReport.OutgoingCount}  |  " +
            $"Bekleyen: {_lastReport.PendingCount}  |  Teslim: {_lastReport.DeliveredCount}";
        ReportSummaryBorder.Visibility = Visibility.Visible;
    }

    private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
    {
        DateFromPicker.SelectedDate  = null;
        DateToPicker.SelectedDate    = null;
        DirectionCombo.SelectedIndex = 0;
        KeywordBox.Text              = "";
        CargoCompanyCombo.SelectedIndex = 0;
        StatusCombo.SelectedIndex    = 0;
        NotifStatusCombo.SelectedIndex = 0;
        PriorityCombo.SelectedIndex  = 0;
        VehiclePlateBox.Text         = "";
        TrackingBox.Text             = "";
        ShipmentNoBox.Text           = "";

        ReportGrid.ItemsSource        = null;
        ReportSummaryBorder.Visibility = Visibility.Collapsed;
        ExportResultBorder.Visibility  = Visibility.Collapsed;
        _lastReport                    = null;
    }

    // ── PDF Export ────────────────────────────────────────────────────────

    private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastReport is null)
        {
            _dialogService.ShowWarning("Önce raporu çalıştırın.", "PDF İndir");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title            = "Kargo Raporunu Kaydet",
            Filter           = "PDF Dosyası (*.pdf)|*.pdf",
            FileName         = $"kargo-raporu-{DateTime.Today:yyyy-MM-dd}.pdf",
            DefaultExt       = "pdf",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var exporter = _services.GetRequiredService<ICargoReportPdfExporter>();
            var bytes    = exporter.Export(_lastReport);
            File.WriteAllBytes(dialog.FileName, bytes);

            ExportResultBlock.Text        = $"PDF kaydedildi: {dialog.FileName}";
            ExportResultBorder.Visibility = Visibility.Visible;

            // PDF'i varsayılan görüntüleyici ile aç
            Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"PDF oluşturulamadı: {ex.Message}", "PDF Hatası");
        }
    }

    // ── Rapor Tablosu Çift Tık ───────────────────────────────────────────

    private void ReportGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // TODO: Kargo operasyonlarını açmak için Gelen/Giden Kargolar listesinden çift tıklayın.
        // Tam DTO yüklemesi bu ekrandan henüz yapılmıyor.
        _dialogService.ShowInfo(
            "Kargo operasyonlarını (durum değiştirme, bildirim, etiket) açmak için\n" +
            "Gelen Kargolar veya Giden Kargolar listesinden ilgili kaydı çift tıklayın.",
            "Operasyon Merkezi");
    }

    // ── Pencere Kapat ─────────────────────────────────────────────────────

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}

/// <summary>Grafik çubuğu için UI modeli — BarWidth px genişlik, ColorBrush WPF Brush'ı içerir.</summary>
internal record ChartBarItem(string Label, int Value, System.Windows.Media.SolidColorBrush Color, int BarWidth);
