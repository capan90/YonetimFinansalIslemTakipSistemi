using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.Common;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Health;

public partial class SystemHealthScreen : UserControl
{
    private readonly IHealthCheckService       _healthService;
    private readonly IDialogService            _dialogService;
    private readonly IErrorNotificationService _notifier;
    private readonly bool                      _isAdmin;
    private AppHealthInfo?                     _lastInfo;

    public SystemHealthScreen(IServiceProvider services)
    {
        InitializeComponent();
        _healthService = services.GetRequiredService<IHealthCheckService>();
        _dialogService = services.GetRequiredService<IDialogService>();
        _notifier      = services.GetRequiredService<IErrorNotificationService>();
        // Menü zaten admin-only olsa da pencere seviyesinde de kontrol ederiz (savunma derinliği)
        _isAdmin       = services.GetRequiredService<IUserContext>().HasPermission(PermissionType.CanManageUsers);

        ScreenData.Bind(this,
            load:       LoadAsync,
            initialize: () => { ConfigureAdminControls(); return Task.CompletedTask; });
    }

    // ── Yükleme / Yenileme ──────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        try
        {
            BtnRefresh.IsEnabled    = false;
            ThemeBrush.Apply(StatusBanner, Border.BackgroundProperty, "Theme.Secondary");
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
            ThemeBrush.Apply(StatusBanner, Border.BackgroundProperty, "Theme.Danger");
            StatusLabel.Text        = "Sağlık kontrolü sırasında beklenmeyen bir hata oluştu.";
        }
        finally
        {
            BtnRefresh.IsEnabled = true;
        }
    }

    private void Apply(AppHealthInfo info)
    {
        // Genel durum bandı.
        // Dolu renk + beyaz metin deseni terk edildi: koyu temada Theme.Success
        // açık yeşile (#4ADE80) dönüyor ve üzerindeki açık metin 1.5:1'e düşüyordu.
        // Yerine, iki temada da birlikte ölçülmüş "yumuşak panel + koyu/açık metin"
        // rol çifti kullanılır.
        var (bgToken, fgToken, text, icon) = info.OverallStatus switch
        {
            HealthStatus.Ok      => ("Theme.Success.Background", "Theme.Success.Text",
                                     "Sistem Durumu: Normal", "✓"),
            HealthStatus.Warning => ("Theme.Warning.Background", "Theme.Warning.Text",
                                     "Sistem Durumu: Uyarı — dikkat gerektiren konular mevcut", "⚠"),
            HealthStatus.Error   => ("Theme.Danger.Background",  "Theme.Danger.Text",
                                     "Sistem Durumu: Hata — acil müdahale gerekebilir", "✗"),
            _                    => ("Theme.SurfaceAlt",         "Theme.Text",
                                     "Bilinmiyor", "?")
        };

        ThemeBrush.Apply(StatusBanner,      Border.BackgroundProperty,    bgToken);
        ThemeBrush.Apply(StatusLabel,       TextBlock.ForegroundProperty, fgToken);
        ThemeBrush.Apply(CheckedAtLabel,    TextBlock.ForegroundProperty, fgToken);
        ThemeBrush.Apply(OverallStatusIcon, TextBlock.ForegroundProperty, fgToken);

        StatusLabel.Text       = text;
        OverallStatusIcon.Text = icon;

        CheckedAtLabel.Text = $"Kontrol zamanı: {info.CheckedAt:dd.MM.yyyy HH:mm:ss}";

        AppSection.ItemsSource    = BuildAppRows(info);
        DbSection.ItemsSource     = BuildDbRows(info);
        LogSection.ItemsSource    = BuildLogRows(info);
        BackupSection.ItemsSource = BuildBackupRows(info);
        UpdateSection.ItemsSource = BuildUpdateRows(info);
        NotifSection.ItemsSource  = BuildNotificationRows(info);
    }

    // Admin kontrolü — MainWindow menüsü zaten admin-only olsa da pencere seviyesinde de uygularız
    private void ConfigureAdminControls()
    {
        var vis = _isAdmin ? Visibility.Visible : Visibility.Collapsed;
        BtnTestMail.Visibility    = vis;
        BtnOpenLog.Visibility     = vis;
        BtnOpenBackup.Visibility  = vis;
        BtnOpenPublish.Visibility = vis;
    }

    // ── Satır Oluşturucular ─────────────────────────────────────────────────

    private static List<HealthRowData> BuildAppRows(AppHealthInfo info) =>
    [
        new("Uygulama Sürümü",     info.AppVersion,      RowStatus.None,
            "Çalışan uygulama assembly/version bilgisidir."),
        new("Makine Adı",          info.MachineName,     RowStatus.None),
        new("Windows Kullanıcısı", info.WindowsUserName, RowStatus.None),
        new("Ortam",               info.AppEnvironment,
            info.AppEnvironment.Equals("Production", StringComparison.OrdinalIgnoreCase)
                ? RowStatus.None : RowStatus.Warning,
            "Development veya Production çalışma ortamını gösterir.")
    ];

    private static List<HealthRowData> BuildDbRows(AppHealthInfo info) =>
    [
        new("Bağlantı",
            info.DatabaseCanConnect ? "Başarılı" : "Bağlantı kurulamadı",
            info.DatabaseCanConnect ? RowStatus.Ok : RowStatus.Error,
            "Uygulamanın veritabanı sunucusuna erişip erişemediğini gösterir."),
        new("Veritabanı Adı",      info.DatabaseName, RowStatus.None,
            "Bağlanılan veritabanı adıdır."),
        new("Sunucu",              info.DataSource,   RowStatus.None,
            "Bağlanılan DB host:port bilgisidir; şifre gösterilmez."),
        new("Son Migration",       Truncate(info.LastMigration, 55), RowStatus.None,
            "Veritabanına uygulanmış son EF migration."),
        new("Bekleyen Migration",
            info.PendingMigrationCount == 0 ? "Yok" : $"{info.PendingMigrationCount} migration bekliyor",
            info.PendingMigrationCount == 0 ? RowStatus.Ok : RowStatus.Warning,
            "Kodda olup veritabanına henüz uygulanmamış migration olup olmadığını gösterir.")
    ];

    private static List<HealthRowData> BuildLogRows(AppHealthInfo info) =>
    [
        new("Log Klasörü",
            string.IsNullOrEmpty(info.LogDirectory) ? "Yapılandırılmamış" : info.LogDirectory,
            info.LogDirectoryExists ? RowStatus.Ok : RowStatus.Warning,
            "Teknik hata loglarının yazıldığı klasör."),
        new("Son Log Dosyası",
            string.IsNullOrEmpty(info.LatestLogFile) ? "Bulunamadı" : info.LatestLogFile,
            string.IsNullOrEmpty(info.LatestLogFile) ? RowStatus.Warning : RowStatus.Ok,
            "En son oluşan log dosyası.")
    ];

    private static List<HealthRowData> BuildBackupRows(AppHealthInfo info) =>
    [
        new("Backup Klasörü",
            string.IsNullOrEmpty(info.BackupDirectory) ? "Yapılandırılmamış" : info.BackupDirectory,
            info.BackupDirectoryExists ? RowStatus.Ok : RowStatus.Warning,
            "Veritabanı yedeklerinin tutulacağı klasör."),
        new("Son Backup Dosyası",
            string.IsNullOrEmpty(info.LatestBackupFile) ? "Henüz backup alınmamış" : info.LatestBackupFile,
            string.IsNullOrEmpty(info.LatestBackupFile) ? RowStatus.Warning : RowStatus.Ok,
            "En son bulunan backup dosyası.")
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
            info.VersionJsonExists ? RowStatus.Ok : RowStatus.Warning,
            "ClickOnce manuel güncelleme kontrolünde kullanılan sürüm dosyası."),
        new("Yayımlanan Sürüm",
            string.IsNullOrEmpty(info.LatestPublishedVersion) ? "Okunamadı" : info.LatestPublishedVersion,
            string.IsNullOrEmpty(info.LatestPublishedVersion) ? RowStatus.Warning : RowStatus.None,
            "Publish klasöründeki version.json'daki sürüm numarasıdır.")
    ];

    private static List<HealthRowData> BuildNotificationRows(AppHealthInfo info) =>
    [
        new("Mail Bildirimi",
            info.NotificationsEnabled ? "Aktif" : "Devre Dışı",
            info.NotificationsEnabled ? RowStatus.Ok : RowStatus.None,
            "Kritik hatalarda mail gönderiminin açık/kapalı olduğunu gösterir."),
        new("Sağlayıcı",
            info.NotificationsEnabled && !string.IsNullOrEmpty(info.NotificationProvider)
                ? info.NotificationProvider : "Yok",
            RowStatus.None),
        new("Alıcı Adresi",
            info.NotificationToConfigured ? "Yapılandırılmış" : "Boş",
            info.NotificationToConfigured ? RowStatus.Ok
                : info.NotificationsEnabled ? RowStatus.Warning : RowStatus.None),
        new("SMTP Sunucusu",
            string.IsNullOrEmpty(info.NotificationSmtpHost) ? "Yapılandırılmamış" : info.NotificationSmtpHost,
            string.IsNullOrEmpty(info.NotificationSmtpHost)
                ? (info.NotificationsEnabled ? RowStatus.Warning : RowStatus.None)
                : RowStatus.Ok,
            "Mail bildirimi için yapılandırılan SMTP host bilgisidir; kullanıcı adı/şifre gösterilmez."),
        // Kimlik bilgisi durumu: şifre/kullanıcı adı değerleri asla gösterilmez
        new("Kimlik Bilgileri",
            info.NotificationCredentialsConfigured ? "Yapılandırılmış" : "Eksik",
            info.NotificationCredentialsConfigured ? RowStatus.Ok
                : info.NotificationsEnabled ? RowStatus.Warning : RowStatus.None,
            "SMTP kullanıcı adı ve şifresi ayarlı mı? Değerler gizlidir.")
    ];

    // ── Buton İşleyicileri ───────────────────────────────────────────────────

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async void TestMail_Click(object sender, RoutedEventArgs e)
    {
        BtnTestMail.IsEnabled = false;
        try
        {
            var (success, error) = await _notifier.SendTestAsync();

            if (success)
                _dialogService.ShowSuccess("Test maili başarıyla gönderildi.");
            else
                _dialogService.ShowError(
                    string.IsNullOrEmpty(error) ? "Test maili gönderilemedi." : error);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Test maili gönderiminde beklenmeyen hata");
            _dialogService.ShowError("Test maili gönderiminde beklenmeyen bir hata oluştu.");
        }
        finally
        {
            BtnTestMail.IsEnabled = true;
        }
    }

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
    // Sabit fırçalar KALDIRILDI (#1B5E20 / #E65C00 / #C62828 / #555555).
    // Renk artık veri sınıfının değil, görünümün sorumluluğu: satır şablonu
    // Status'e göre DataTrigger + DynamicResource ile renklendirir. Böylece
    // tema değişiminde açık listedeki satırlar da anında güncellenir.

    public HealthRowData(string label, string value, RowStatus status, string? tooltip = null)
    {
        Label   = label;
        Value   = value;
        Status  = status;
        Tooltip = tooltip;
    }

    public string    Label   { get; }
    public string    Value   { get; }
    public RowStatus Status  { get; }
    public string?   Tooltip { get; }

    public string StatusText => Status switch
    {
        RowStatus.Ok      => "OK",
        RowStatus.Warning => "Uyarı",
        RowStatus.Error   => "Hata",
        _                 => ""
    };

}
