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
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.Common;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

/// <summary>
/// Kargo panosu ekranı.
///
/// BU EKRAN ESKİDEN İKİNCİ BİR KABUKTU: kendi navigasyon şeridi, yardım
/// menüsü ve çıkış düğmesi vardı. Kabuğa taşındığında bunlar gizleniyordu;
/// Faz F1'de barındırıcı pencereyle birlikte tamamen kaldırıldı. Ekran artık
/// yalnızca PANO — navigasyon kabuğun işi.
/// </summary>
public partial class CargoDashboardScreen : UserControl, IShellCloseSource, IShellNavigationAware
{
    /// <summary>
    /// Alt diyalogların sahibi AĞAÇTAN bulunur: ekran kabuk sekmesinde
    /// barınıyor ve sahibi kabuk penceresidir. Sabit bir pencereye
    /// bağlanırsa sahipsiz diyalog açardı.
    /// </summary>
    private Window? HostWindow => Window.GetWindow(this);

    /// <summary>Kabuk sekme oluştururken atar (bkz. ShellViewModel.Attach).</summary>
    public IShellNavigator? Navigator { get; set; }

    /// <summary>Kapanma isteği — kabukta sekmeyi kapatır.</summary>
    public event Action? CloseRequested;

    private readonly IServiceProvider     _services;
    private readonly IDialogService       _dialogService;
    private CargoReportDto?               _lastReport;

    // ComboBox item sarmalayıcıları
    private record ComboItem<T>(string Label, T? Value);

    // Grafiklerin ham verisi. Seriler bundan üretilir; tema değişiminde
    // yeniden sorgu atmadan yeniden boyanabilsin diye saklanır.
    private IReadOnlyList<(string Label, double Value)> _directionData = [];
    private IReadOnlyList<(string Label, double Value)> _statusData    = [];
    private IReadOnlyList<(string Label, double Value)> _companyData   = [];

    private static IReadOnlyList<(string Label, double Value)> ToPairs(
        IReadOnlyList<CargoDashboardChartItem> items)
        => items.Select(i => (i.Label, (double)i.Value)).ToList();

    public CargoDashboardScreen(IServiceProvider services)
    {
        InitializeComponent();
        _services      = services;
        _dialogService = services.GetRequiredService<IDialogService>();

        // LiveCharts SkiaSharp ile çizer, DynamicResource'u görmez: tema
        // değişince seriler elle yeniden boyanmalı (yeniden sorgu atmadan).
        // Abonelik Loaded/Unloaded ile SİMETRİK (bkz. BindThemeRepaint).
        ScreenData.BindThemeRepaint(this, RebuildCharts);

        // Filtre kutuları ve kargo firması listesi YALNIZCA ilk gösterimde
        // doldurulur; yenilemede tekrarlanırsa kullanıcının seçtiği filtre
        // sıfırlanır ve yenile "filtremi sil" anlamına gelirdi.
        ScreenData.Bind(this,
            load:       () => GuardedAsync(LoadDashboardAsync),
            initialize: () => GuardedAsync(InitializeAsync));
    }

    /// <summary>
    /// İlk gösterim hazırlığı. Panonun verisi buraya girmez — o yenilenebilir
    /// olmalı (bkz. <see cref="ScreenData"/>).
    /// </summary>
    private async Task InitializeAsync()
    {
        PopulateFilterCombos();
        await LoadCargoCompaniesAsync();

        // Açılışta güncelleme kontrolü BURADA DEĞİL: uygulama seviyesi bir iş
        // ve artık tek yerde, ShellWindow'da yürüyor. Bu ekran yalnızca ince
        // barındırıcı pencerede barınırken kendi kontrolünü yapıyordu; o
        // pencere kaldırıldı (Faz F1).
    }

    /// <summary>
    /// Pano yükleme hatalarının ortak kapısı. Teknik detay diyaloga yazılmaz
    /// (bağlantı bilgisi sızabilir); log'a gider.
    /// </summary>
    private async Task GuardedAsync(Func<Task> work)
    {
        try
        {
            await work();
        }
        catch (Exception ex)
        {
            _ = _services.GetRequiredService<ISystemLogService>()
                .LogErrorAsync("Cargo", "Dashboard yüklenirken hata oluştu", ex, source: nameof(CargoDashboardScreen));
            _dialogService.ShowError("Dashboard yüklenirken hata oluştu. Ayrıntılar sistem loguna kaydedildi.", "Dashboard Hatası");
        }
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

        // Grafikler — ham veri saklanır, seriler ondan kurulur
        _directionData = ToPairs(dto.DirectionChart);
        _statusData    = ToPairs(dto.StatusChart);
        _companyData   = ChartPalette.GroupSmall(ToPairs(dto.CompanyChart), keep: 5);
        RebuildCharts();

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

        // Ham veri saklanır; seriler tema değişiminde bundan yeniden kurulur.
        // Beşten fazla firma gelirse dördüncü renk üretmek yerine "Diğer"de toplanır.
        _directionData = ToPairs(dto.DirectionChart);
        _statusData    = ToPairs(dto.StatusChart);
        _companyData   = ChartPalette.GroupSmall(ToPairs(dto.CompanyChart), keep: 5);

        RebuildCharts();
        RecentGrid.ItemsSource = dto.RecentShipments;
    }

    // ── Grafikler ─────────────────────────────────────────────────────────
    //
    // Elle çizilen Rectangle barlar LiveCharts'a taşındı. DTO'daki
    // CargoDashboardChartItem.Color alanı BİLİNÇLİ OLARAK yok sayılıyor:
    // Application katmanından gelen keyfi hex'lerdi (turkuaz/mor/macenta yan
    // yana) ve tema sözlüğünü görmüyorlardı. Renk artık ChartPalette'ten,
    // verinin TÜRÜNE göre seçilir.

    /// <summary>
    /// Tüm dashboard grafiklerini aktif tema renkleriyle kurar.
    /// Veri yüklendiğinde ve tema değiştiğinde çağrılır.
    /// </summary>
    private void RebuildCharts()
    {
        BuildDirectionChart();
        BuildStatusChart();
        BuildCompanyChart();
    }

    /// <summary>
    /// Gelen / Giden — İKİ KATEGORİ, yön bilgisi taşır.
    /// Kırmızı/yeşil değil mavi–turuncu; iki seri olduğu için legend zorunlu.
    /// </summary>
    private void BuildDirectionChart()
    {
        var data = _directionData;
        if (data.Count == 0) { DirectionChart.Series = []; return; }

        DirectionChart.Series = data.Select((item, i) => (ISeries)new ColumnSeries<double>
        {
            Name   = item.Label,
            Values = [item.Value],
            Fill   = ChartPalette.Fill(i == 0 ? ChartPalette.Inflow() : ChartPalette.Outflow()),
        }).ToArray();

        DirectionChart.XAxes = [CategoryAxis([""])];
        DirectionChart.YAxes = [ValueAxis()];
    }

    /// <summary>
    /// Durum Dağılımı — TEK BOYUTLU BÜYÜKLÜK verisi.
    /// Burada renk kimlik değil büyüklük taşır: kategorik palet kullanılmaz,
    /// tek hue'nun açık→koyu adımları kullanılır. Kategori sayısı paletle
    /// sınırlı değildir çünkü ayrım renkle değil sırayla yapılır.
    /// </summary>
    private void BuildStatusChart()
    {
        var data = _statusData;
        if (data.Count == 0) { StatusChart.Series = []; return; }

        // Büyükten küçüğe: skala adımı sıralamayla anlam kazanır
        var ordered = data.OrderByDescending(d => d.Value).ToList();

        StatusChart.Series =
        [
            new ColumnSeries<double>
            {
                Name    = "Kayıt",
                Values  = ordered.Select(d => (double)d.Value).ToArray(),
                // Skalanın açık ucu yüzeyle düşük kontrastlı; görünürlüğü kenarlık taşır
                Stroke  = ChartPalette.Stroke(ChartPalette.SequentialStroke(), 1f),
                Fill    = ChartPalette.Fill(ChartPalette.Sequential(ordered.Count - 1, ordered.Count)),
            }
        ];

        StatusChart.XAxes = [CategoryAxis(ordered.Select(d => d.Label).ToArray())];
        StatusChart.YAxes = [ValueAxis()];
    }

    /// <summary>
    /// Top 5 Kargo Firması — yine büyüklük verisi, kimlik değil.
    /// Beşten fazla firma gelirse ChartPalette.GroupSmall devreye girer;
    /// dördüncü bir kategorik renk ÜRETİLMEZ.
    /// </summary>
    private void BuildCompanyChart()
    {
        var data = _companyData;
        if (data.Count == 0) { CompanyChart.Series = []; return; }

        var ordered = data.OrderByDescending(d => d.Value).ToList();

        CompanyChart.Series =
        [
            new ColumnSeries<double>
            {
                Name   = "Gönderi",
                Values = ordered.Select(d => (double)d.Value).ToArray(),
                Stroke = ChartPalette.Stroke(ChartPalette.SequentialStroke(), 1f),
                Fill   = ChartPalette.Fill(ChartPalette.Sequential(ordered.Count - 1, ordered.Count)),
            }
        ];

        CompanyChart.XAxes = [CategoryAxis(ordered.Select(d => d.Label).ToArray())];
        CompanyChart.YAxes = [ValueAxis()];
    }

    private static Axis CategoryAxis(string[] labels) => new()
    {
        Labels          = labels,
        LabelsPaint     = ChartPalette.Fill(ChartPalette.AxisText()),
        TextSize        = 11,
        SeparatorsPaint = null,
        LabelsRotation  = labels.Length > 4 ? 30 : 0,
    };

    private static Axis ValueAxis() => new()
    {
        LabelsPaint     = ChartPalette.Fill(ChartPalette.AxisText()),
        TextSize        = 11,
        SeparatorsPaint = ChartPalette.Stroke(ChartPalette.GridLine(), 1),
        MinLimit        = 0,
        Labeler         = v => v.ToString("N0"),
    };

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
            new ComboItem<CargoShipmentStatus?>("Tümü",                     null),
            new ComboItem<CargoShipmentStatus?>("Gönderime Hazır",          CargoShipmentStatus.Prepared),
            new ComboItem<CargoShipmentStatus?>("Kargoya Teslim Edildi",    CargoShipmentStatus.HandedToCargo),
            new ComboItem<CargoShipmentStatus?>("Bekleniyor",               CargoShipmentStatus.Waiting),
            new ComboItem<CargoShipmentStatus?>("Gönderildi",               CargoShipmentStatus.Shipped),
            new ComboItem<CargoShipmentStatus?>("Teslim Alındı",            CargoShipmentStatus.Received),
            new ComboItem<CargoShipmentStatus?>("Personele Teslim Edildi",  CargoShipmentStatus.PersonnelDelivered),
            new ComboItem<CargoShipmentStatus?>("Teslim Edildi",            CargoShipmentStatus.Delivered),
            new ComboItem<CargoShipmentStatus?>("İptal",                    CargoShipmentStatus.Cancelled),
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

    /// <summary>
    /// Sayaç kartına tıklama → raporu o kartın saydığı kayıtlara filtreler
    /// (Faz C bonus). Yalnızca "Bugün Gelen" ve "Bugün Giden" için bağlıdır;
    /// gerekçesi XAML'de yazılı — diğer kartların kümesi mevcut filtre
    /// alanlarıyla birebir üretilemiyor ve farklı bir liste açmak karttaki
    /// rakama olan güveni bozardı.
    ///
    /// Mevcut filtre alanlarını doldurup normal rapor yolunu kullanır;
    /// yeni sorgu veya iş mantığı yazılmadı.
    /// </summary>
    private async void CountCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string direction }) return;

        ClearFilterButton_Click(sender, e);

        DateFromPicker.SelectedDate = DateTime.Today;
        DateToPicker.SelectedDate   = DateTime.Today;

        // "Tümü" 0. sırada; Gelen 1, Giden 2 (bkz. PopulateFilterCombos)
        DirectionCombo.SelectedIndex = direction == "Incoming" ? 1 : 2;

        try
        {
            await RunReportAsync();
            // Rapor tablosu sayfanın altında; kullanıcı tıkladığı sonucu görsün
            ReportGrid.BringIntoView();
        }
        catch (Exception ex)
        {
            _ = _services.GetRequiredService<ISystemLogService>()
                .LogErrorAsync("Cargo", "Sayaç kartından rapor alınırken hata oluştu", ex,
                               source: nameof(CargoDashboardScreen));
            _dialogService.ShowError("Rapor alınırken hata oluştu. Ayrıntılar sistem loguna kaydedildi.", "Rapor Hatası");
        }
    }

    private async void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RunReportAsync();
        }
        catch (Exception ex)
        {
            // Npgsql mesajları sunucu/şema bilgisi sızdırabilir — detay log'a, kullanıcıya jenerik mesaj
            _ = _services.GetRequiredService<ISystemLogService>()
                .LogErrorAsync("Cargo", "Kargo raporu alınırken hata oluştu", ex, source: nameof(CargoDashboardScreen));
            _dialogService.ShowError("Rapor alınırken hata oluştu. Ayrıntılar sistem loguna kaydedildi.", "Rapor Hatası");
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
            _ = _services.GetRequiredService<ISystemLogService>()
                .LogErrorAsync("Cargo", "Kargo raporu PDF oluşturulamadı", ex, source: nameof(CargoDashboardScreen));
            _dialogService.ShowError("PDF oluşturulamadı. Ayrıntılar sistem loguna kaydedildi.", "PDF Hatası");
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

    // Bu ekranın kendi navigasyon şeridi (Gelen/Giden/Rehber düğmeleri, yardım
    // menüsü, çıkış) KALDIRILDI (Faz F1). Şerit yalnızca ince barındırıcı
    // pencerede görünüyordu; kabukta her zaman gizliydi çünkü kabuğun kendi
    // navigasyon rayı, araç bloğu ve çıkış düğmesi var — ikisi birden ikinci
    // bir kabuk kurmak olurdu. Barındırıcı pencere silindiği için şerit,
    // görünürlük kuralları ve yedek pencere fabrikaları da gitti.

    // Kişisel ayarlar (Mail Ayarlarım, Harf Duyarlılığı), güncelleme denetimi,
    // log klasörü ve çıkış BU EKRANDAN KALDIRILDI (Faz F1). Hepsi kabuğun
    // "Araçlar" bloğunda ve navigasyon rayında zaten var; buradaki kopyaları
    // yalnızca ince barındırıcı pencere yolunda erişilebiliyordu.

    private void CloseButton_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke();
}

// ChartBarItem KALDIRILDI (Faz C). Elle çizilen Rectangle barların UI modeliydi;
// px genişliği kendisi hesaplıyor ve rengi Application katmanındaki hex'ten
// alıyordu. Barlar LiveCharts'a taşındı, renk ChartPalette'ten geliyor.
