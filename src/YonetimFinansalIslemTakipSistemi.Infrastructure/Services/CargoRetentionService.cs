using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// 6 aydan eski kargo kayıtlarını DB'den kalıcı (hard delete) olarak temizler.
/// Günde en fazla bir kez çalışır; son çalışma zamanı ApplicationSettings'te saklanır.
/// Uygulama başlangıcında arka planda tetiklenir — UI'ı bloklamamak için fire-and-forget.
/// </summary>
public class CargoRetentionService : ICargoRetentionService
{
    private const string EnabledKey    = "CargoRetention:Enabled";
    private const string MonthsKey     = "CargoRetention:MonthsToKeep";
    private const string LastRunKey    = "CargoRetention:LastRunUtc";
    private const string SystemUserId  = "00000000-0000-0000-0000-000000000000"; // sistem işlemi

    private readonly IServiceProvider          _services;
    private readonly ISystemLogService         _systemLog;
    private readonly IMailSettingsService      _mailSettings;
    private readonly ICargoDashboardCacheService _dashboardCache;

    public CargoRetentionService(
        IServiceProvider           services,
        ISystemLogService          systemLog,
        IMailSettingsService       mailSettings,
        ICargoDashboardCacheService dashboardCache)
    {
        _services       = services;
        _systemLog      = systemLog;
        _mailSettings   = mailSettings;
        _dashboardCache = dashboardCache;
    }

    public async Task RunAsync()
    {
        try
        {
            await RunInternalAsync();
        }
        catch (Exception ex)
        {
            // Üst seviye güvenlik ağı — bu catch'e normalde düşülmemeli
            await _systemLog.LogCriticalAsync(
                "CargoRetention",
                "Kargo temizleme servisi beklenmedik şekilde çöktü.",
                ex,
                source: nameof(CargoRetentionService));
        }
    }

    private async Task RunInternalAsync()
    {
        using var scope = _services.CreateScope();
        var settingsRepo = scope.ServiceProvider.GetRequiredService<IApplicationSettingRepository>();
        var db           = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Özellik devre dışıysa çalışma
        var enabledSetting = await settingsRepo.GetByKeyAsync(EnabledKey);
        if (enabledSetting?.Value?.Equals("false", StringComparison.OrdinalIgnoreCase) == true)
            return;

        // Bugün zaten çalıştıysa tekrar çalışma
        var lastRunSetting = await settingsRepo.GetByKeyAsync(LastRunKey);
        if (lastRunSetting?.Value is not null
            && DateTime.TryParse(lastRunSetting.Value, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var lastRun)
            && lastRun.Date == DateTime.UtcNow.Date)
        {
            return;
        }

        var monthsToKeep = 6;
        var monthsSetting = await settingsRepo.GetByKeyAsync(MonthsKey);
        if (monthsSetting?.Value is not null && int.TryParse(monthsSetting.Value, out var m) && m > 0)
            monthsToKeep = m;

        var cutoff     = DateTime.UtcNow.AddMonths(-monthsToKeep);
        var systemGuid = Guid.Empty;

        // Tüm kayıtları sil (soft-deleted dahil) — query filter ham SQL'e uygulanmaz
        var deleted = await db.Database.ExecuteSqlAsync(
            $"""DELETE FROM cargo_shipments WHERE "ShipmentDate" < {cutoff}""");

        // Son çalışma zamanını güncelle
        await settingsRepo.UpsertAsync(
            LastRunKey,
            DateTime.UtcNow.ToString("O"),
            isEncrypted: false,
            userId: systemGuid);

        if (deleted == 0)
        {
            await _systemLog.LogInfoAsync(
                "CargoRetention",
                "Kargo kayıt temizliği çalıştı. Silinecek kayıt bulunamadı.",
                source: nameof(CargoRetentionService));
            return;
        }

        // Silme yapıldıysa dashboard cache'ini geçersiz kıl
        _dashboardCache.Invalidate();

        var msg = $"Kargo kayıt temizliği tamamlandı. {monthsToKeep} aydan eski {deleted} kayıt kalıcı olarak silindi. Cutoff: {cutoff:dd.MM.yyyy}";

        await _systemLog.LogWarningAsync(
            "CargoRetention",
            msg,
            source: nameof(CargoRetentionService));

        // Mail bildirimi — fire-and-forget; başarısız olursa uygulama etkilenmez
        _ = Task.Run(async () =>
        {
            try
            {
                await SendRetentionMailAsync(deleted, cutoff, msg);
            }
            catch (Exception ex)
            {
                // Sonsuz döngü engeli: LogErrorAsync mail tetiklemez (sadece LogCriticalAsync tetikler)
                await _systemLog.LogErrorAsync(
                    "CargoRetention",
                    $"Kargo temizleme mail bildirimi gönderilemedi: {ex.Message}",
                    ex,
                    source: nameof(CargoRetentionService));
            }
        });
    }

    private async Task SendRetentionMailAsync(int deletedCount, DateTime cutoff, string logMessage)
    {
        var settings = await _mailSettings.GetAsync();
        if (settings is null || string.IsNullOrWhiteSpace(settings.SmtpHost))
            return; // mail yapılandırması yok — sessizce geç

        var appVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "?";
        var now        = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

        var subject = "Kargo Kayıt Temizliği Tamamlandı";
        var body = $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family:Arial,sans-serif;font-size:13px;color:#333;max-width:700px;">
              <h2 style="color:#1B5E20;border-bottom:2px solid #1B5E20;padding-bottom:6px;">
                ✓ Kargo Kayıt Temizliği Raporu
              </h2>
              <table style="border-collapse:collapse;width:100%;margin-bottom:16px;">
                <tr style="background:#F5F5F5;"><td style="padding:5px 10px;width:200px;font-weight:bold;">Çalışma Zamanı</td><td style="padding:5px 10px;">{now}</td></tr>
                <tr><td style="padding:5px 10px;font-weight:bold;">Makine Adı</td><td style="padding:5px 10px;">{WebUtility.HtmlEncode(Environment.MachineName)}</td></tr>
                <tr style="background:#F5F5F5;"><td style="padding:5px 10px;font-weight:bold;">Uygulama Sürümü</td><td style="padding:5px 10px;">{WebUtility.HtmlEncode(appVersion)}</td></tr>
                <tr><td style="padding:5px 10px;font-weight:bold;">Cutoff Tarihi</td><td style="padding:5px 10px;">{cutoff:dd.MM.yyyy}</td></tr>
                <tr style="background:#F5F5F5;"><td style="padding:5px 10px;font-weight:bold;">Silinen Kayıt Sayısı</td><td style="padding:5px 10px;color:#C62828;font-weight:bold;">{deletedCount:N0}</td></tr>
              </table>
              <p style="background:#E8F5E9;padding:10px;border-left:4px solid #4CAF50;">{WebUtility.HtmlEncode(logMessage)}</p>
              <hr style="border:none;border-top:1px solid #DDD;margin-top:20px;"/>
              <p style="color:#999;font-size:11px;">Bu e-posta Yönetim Finansal İşlem Takip Sistemi tarafından otomatik gönderilmiştir.</p>
            </body>
            </html>
            """;

        var fromAddr = string.IsNullOrWhiteSpace(settings.SenderEmail)
            ? settings.Username
            : settings.SenderEmail;

        using var mail = new MailMessage(fromAddr, settings.Username, subject, body)
        {
            IsBodyHtml = true
        };

        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl             = settings.EnableSsl,
            DeliveryMethod        = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Timeout               = 15_000
        };

        if (!string.IsNullOrWhiteSpace(settings.Username))
            client.Credentials = new NetworkCredential(settings.Username, settings.Password);

        await client.SendMailAsync(mail);
    }
}
