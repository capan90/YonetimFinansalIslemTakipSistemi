using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Health;

public partial class SystemHealthWindow : Window
{
    private readonly IHealthCheckService _healthService;
    private readonly IDialogService      _dialogService;
    private AppHealthInfo?               _lastInfo;

    public SystemHealthWindow(IServiceProvider services)
    {
        InitializeComponent();
        _healthService = services.GetRequiredService<IHealthCheckService>();
        _dialogService = services.GetRequiredService<IDialogService>();
        Loaded += async (_, _) => await LoadAsync();
    }

    // ── Yükleme / Yenileme ──────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        try
        {
            BtnRefresh.IsEnabled    = false;
            StatusBanner.Background = Brushes.DimGray;
            StatusLabel.Text        = "Kontrol ediliyor...";
            CheckedAtLabel.Text     = "";
            OverallStatusIcon.Text  = "●";

            var info = await _healthService.GetHealthAsync();
            _lastInfo = info;
            Apply(info);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Sistem sağlık kontrolü başarısız");
            StatusBanner.Background = HealthRowData.ErrBrush;
            StatusLabel.Text        = "Sağlık kontrolü sırasında beklenmeyen bir hata oluştu.";
        }
        finally
        {
            BtnRefresh.IsEnabled = true;
        }
    }

    private void Apply(AppHealthInfo info)
    {
        // Genel durum bandı
        (StatusBanner.Background, StatusLabel.Text, OverallStatusIcon.Text) = info.OverallStatus switch
        {
            HealthStatus.Ok      => (HealthRowData.OkBrush,   "Sistem Durumu: Normal",                                       "✓"),
            HealthStatus.Warning => (HealthRowData.WarnBrush,  "Sistem Durumu: Uyarı — dikkat gerektiren konular mevcut",     "⚠"),
            HealthStatus.Error   => (HealthRowData.ErrBrush,   "Sistem Durumu: Hata — acil müdahale gerekebilir",             "✗"),
            _                    => ((Brush)Brushes.DimGray,   "Bilinmiyor",                                                  "?")
        };

        CheckedAtLabel.Text = $"Kontrol zamanı: {info.CheckedAt:dd.MM.yyyy HH:mm:ss}";

        AppSection.ItemsSource    = BuildAppRows(info);
        DbSection.ItemsSource     = BuildDbRows(info);
        LogSection.ItemsSource    = BuildLogRows(info);
        BackupSection.ItemsSource = BuildBackupRows(info);
        UpdateSection.ItemsSource = BuildUpdateRows(info);
    }

    // ── Satır Oluşturucular ─────────────────────────────────────────────────

    private static List<HealthRowData> BuildAppRows(AppHealthInfo info) =>
    [
        new("Uygulama Sürümü",     info.AppVersion,      RowStatus.None),
        new("Makine Adı",          info.MachineName,     RowStatus.None),
        new("Windows Kullanıcısı", info.WindowsUserName, RowStatus.None),
        new("Ortam",               info.AppEnvironment,
            info.AppEnvironment.Equals("Production", StringComparison.OrdinalIgnoreCase)
                ? RowStatus.None : RowStatus.Warning)
    ];

    private static List<HealthRowData> BuildDbRows(AppHealthInfo info) =>
    [
        new("Bağlantı",
            info.DatabaseCanConnect ? "Başarılı" : "Bağlantı kurulamadı",
            info.DatabaseCanConnect ? RowStatus.Ok : RowStatus.Error),
        new("Veritabanı Adı",      info.DatabaseName, RowStatus.None),
        new("Sunucu",              info.DataSource,   RowStatus.None),
        new("Son Migration",       Truncate(info.LastMigration, 55), RowStatus.None),
        new("Bekleyen Migration",
            info.PendingMigrationCount == 0 ? "Yok" : $"{info.PendingMigrationCount} migration bekliyor",
            info.PendingMigrationCount == 0 ? RowStatus.Ok : RowStatus.Warning)
    ];

    private static List<HealthRowData> BuildLogRows(AppHealthInfo info) =>
    [
        new("Log Klasörü",
            string.IsNullOrEmpty(info.LogDirectory) ? "Yapılandırılmamış" : info.LogDirectory,
            info.LogDirectoryExists ? RowStatus.Ok : RowStatus.Warning),
        new("Son Log Dosyası",
            string.IsNullOrEmpty(info.LatestLogFile) ? "Bulunamadı" : info.LatestLogFile,
            string.IsNullOrEmpty(info.LatestLogFile) ? RowStatus.Warning : RowStatus.Ok)
    ];

    private static List<HealthRowData> BuildBackupRows(AppHealthInfo info) =>
    [
        new("Backup Klasörü",
            string.IsNullOrEmpty(info.BackupDirectory) ? "Yapılandırılmamış" : info.BackupDirectory,
            info.BackupDirectoryExists ? RowStatus.Ok : RowStatus.Warning),
        new("Son Backup Dosyası",
            string.IsNullOrEmpty(info.LatestBackupFile) ? "Bulunamadı" : info.LatestBackupFile,
            string.IsNullOrEmpty(info.LatestBackupFile) ? RowStatus.Warning : RowStatus.Ok)
    ];

    private static List<HealthRowData> BuildUpdateRows(AppHealthInfo info) =>
    [
        new("Yayın Klasörü",
            string.IsNullOrEmpty(info.UpdatePublishPath) ? "Yapılandırılmamış" : info.UpdatePublishPath,
            RowStatus.None),
        new("Version.json Yolu",
            string.IsNullOrEmpty(info.VersionJsonPath) ? "—" : info.VersionJsonPath,
            RowStatus.None),
        new("Version.json Durumu",
            info.VersionJsonExists ? "Mevcut" : "Bulunamadı",
            info.VersionJsonExists ? RowStatus.Ok : RowStatus.Warning),
        new("Yayımlanan Sürüm",
            string.IsNullOrEmpty(info.LatestPublishedVersion) ? "Okunamadı" : info.LatestPublishedVersion,
            string.IsNullOrEmpty(info.LatestPublishedVersion) ? RowStatus.Warning : RowStatus.None)
    ];

    // ── Buton İşleyicileri ───────────────────────────────────────────────────

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private void OpenLog_Click(object sender, RoutedEventArgs e)
        => OpenDirectory(
            _lastInfo?.LogDirectory ?? App.LogDirectory,
            "Log klasörü henüz oluşturulmadı. Uygulama log yazmaya başladığında otomatik oluşturulur.");

    private void OpenBackup_Click(object sender, RoutedEventArgs e)
        => OpenDirectory(
            _lastInfo?.BackupDirectory ?? "",
            "Backup klasörü bulunamadı. Backup scripti çalıştırıldığında oluşturulur.");

    private void OpenPublish_Click(object sender, RoutedEventArgs e)
        => OpenDirectory(
            _lastInfo?.UpdatePublishPath ?? "",
            "Yayın klasörüne erişilemiyor. YONETIM_UPDATE_PATH ortam değişkenini veya DeploymentSettings'i kontrol edin.");

    private void OpenDirectory(string dir, string missingMessage)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
        {
            _dialogService.ShowWarning(missingMessage);
            return;
        }
        Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
    }

    private static string Truncate(string s, int max)
        => string.IsNullOrEmpty(s) || s.Length <= max ? s : "..." + s[^(max - 3)..];
}

// ── Yardımcı Tipler ─────────────────────────────────────────────────────────

internal enum RowStatus { None, Ok, Warning, Error }

internal sealed class HealthRowData
{
    // Statik fırçalar: tüm satırlar aynı örneği paylaşır; freeze edildi (thread-safe, performanslı)
    internal static readonly SolidColorBrush OkBrush   = CreateFrozen(0x1B, 0x5E, 0x20);
    internal static readonly SolidColorBrush WarnBrush  = CreateFrozen(0xE6, 0x5C, 0x00);
    internal static readonly SolidColorBrush ErrBrush   = CreateFrozen(0xC6, 0x28, 0x28);
    private  static readonly SolidColorBrush NoneBrush  = CreateFrozen(0x55, 0x55, 0x55);

    public HealthRowData(string label, string value, RowStatus status)
    {
        Label  = label;
        Value  = value;
        Status = status;
    }

    public string    Label  { get; }
    public string    Value  { get; }
    public RowStatus Status { get; }

    public string StatusText => Status switch
    {
        RowStatus.Ok      => "OK",
        RowStatus.Warning => "Uyarı",
        RowStatus.Error   => "Hata",
        _                 => ""
    };

    public Brush StatusBrush => Status switch
    {
        RowStatus.Ok      => OkBrush,
        RowStatus.Warning => WarnBrush,
        RowStatus.Error   => ErrBrush,
        _                 => NoneBrush
    };

    private static SolidColorBrush CreateFrozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
