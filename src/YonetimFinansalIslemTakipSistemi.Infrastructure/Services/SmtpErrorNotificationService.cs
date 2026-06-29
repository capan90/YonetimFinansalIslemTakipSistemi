using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using Microsoft.Extensions.Logging;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// SMTP tabanlı hata bildirimi. appsettings.json ErrorNotifications:Enabled = true ise çalışır.
/// Env var YONETIM_SMTP_USERNAME ve YONETIM_SMTP_PASSWORD ayarları config üzerine yazar.
/// Aynı hata tipi/mesajı için 5 dakika cooldown uygulanır (spam engeli).
/// Mail gönderimi asenkron ve fire-and-forget; uygulama asla bloke olmaz.
/// </summary>
public class SmtpErrorNotificationService : IErrorNotificationService
{
    private static readonly TimeSpan CooldownPeriod = TimeSpan.FromMinutes(5);

    private readonly SmtpNotificationOptions _options;
    private readonly ILogger<SmtpErrorNotificationService> _logger;
    private readonly ISystemLogService _systemLog;
    private readonly ConcurrentDictionary<string, DateTime> _cooldown = new();
    private bool _configWarningLogged;

    public SmtpErrorNotificationService(
        SmtpNotificationOptions options,
        ILogger<SmtpErrorNotificationService> logger,
        ISystemLogService systemLog)
    {
        _options   = options;
        _logger    = logger;
        _systemLog = systemLog;
    }

    public Task NotifyAsync(string message, Exception? exception = null, NotificationContext? context = null)
    {
        if (!_options.Enabled)
            return Task.CompletedTask;

        // Yapılandırma doğrulama — sadece ilk sorunlu çağrıda uyar
        if (!IsConfigValid(out var reason))
        {
            if (!_configWarningLogged)
            {
                _logger.LogWarning("SmtpErrorNotificationService devre dışı: {Reason}", reason);
                _ = _systemLog.LogWarningAsync("Mail", $"Mail bildirimi yapılandırması eksik: {reason}", "SmtpErrorNotificationService");
                _configWarningLogged = true;
            }
            return Task.CompletedTask;
        }

        // Spam engeli: aynı fingerprint için 5 dk bekleme
        var fingerprint = BuildFingerprint(exception, message);
        if (!TryMarkSent(fingerprint))
        {
            _logger.LogDebug("Hata bildirimi cooldown: {Fingerprint}", fingerprint);
            return Task.CompletedTask;
        }

        // Fire-and-forget — caller bloke olmaz, iç hata uygulamayı etkilemez
        _ = Task.Run(async () =>
        {
            try
            {
                var subject = "[Yönetim Finansal İşlem Takip Sistemi] Kritik Hata";
                var body    = BuildHtmlBody(message, exception, context);
                await SendCoreAsync(subject, body);
                _logger.LogDebug("Hata bildirimi gönderildi: {To}", _options.To);
            }
            catch (Exception ex)
            {
                // Mail hatası asla uygulamayı etkilemez; SystemLogService döngü oluşturmamak için
                // LogWarningAsync kullanır (LogCriticalAsync tekrar mail göndermeye çalışmaz)
                _logger.LogWarning(ex, "Hata bildirimi gönderilemedi — SMTP hatası");
                _ = _systemLog.LogErrorAsync("Mail", $"Hata bildirimi e-postası gönderilemedi: {ex.Message}", ex, "SmtpErrorNotificationService");
            }
        });

        return Task.CompletedTask;
    }

    // ── Test gönderimi ───────────────────────────────────────────────────────

    public async Task<(bool Success, string? Error)> SendTestAsync()
    {
        if (!_options.Enabled)
            return (false, "Mail bildirimi devre dışı (ErrorNotifications:Enabled = false)");

        if (!IsConfigValid(out var reason))
            return (false, reason);

        try
        {
            var subject = "[Yönetim Finansal İşlem Takip Sistemi] Test Maili";
            var body    = $"""
                <html><body style="font-family:Arial,sans-serif;font-size:13px;">
                  <h3 style="color:#1B5E20;">✓ SMTP Yapılandırması Çalışıyor</h3>
                  <p>Bu bir test mailidir. Bildirim altyapınız düzgün çalışmaktadır.</p>
                  <p><small>Makine: {WebUtility.HtmlEncode(Environment.MachineName)} — Tarih: {DateTime.Now:dd.MM.yyyy HH:mm:ss}</small></p>
                </body></html>
                """;
            await SendCoreAsync(subject, body);
            _logger.LogInformation("Test maili gönderildi: {To}", _options.To);
            return (true, null);
        }
        catch (Exception ex)
        {
            // Stack trace gizlenir; sadece kısa hata mesajı döner
            _logger.LogWarning(ex, "SMTP test maili gönderilemedi");
            return (false, ex.Message);
        }
    }

    // ── SMTP gönderim ────────────────────────────────────────────────────────

    private async Task SendCoreAsync(string subject, string body)
    {
        var fromAddr = string.IsNullOrWhiteSpace(_options.From)
            ? _options.SmtpUsername
            : _options.From;

        using var mail = new MailMessage(fromAddr, _options.To, subject, body)
        {
            IsBodyHtml = true
        };

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl             = _options.SmtpEnableSsl,
            DeliveryMethod        = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Timeout               = 15_000  // 15 saniye; mail sunucusu yavaş ise bloke etmemek için
        };

        if (!string.IsNullOrWhiteSpace(_options.SmtpUsername))
        {
            client.Credentials = new NetworkCredential(
                _options.SmtpUsername,
                _options.SmtpPassword);
        }

        await client.SendMailAsync(mail);
    }

    // ── Yardımcı metodlar ────────────────────────────────────────────────────

    private bool IsConfigValid(out string reason)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
        {
            reason = "SMTP host boş (ErrorNotifications:Smtp:Host veya appsettings.json)";
            return false;
        }
        if (string.IsNullOrWhiteSpace(_options.To))
        {
            reason = "Alıcı (To) adresi boş (ErrorNotifications:To)";
            return false;
        }
        if (!string.IsNullOrWhiteSpace(_options.SmtpUsername)
            && string.IsNullOrWhiteSpace(_options.SmtpPassword))
        {
            reason = "SMTP kullanıcı adı var ama şifre boş (YONETIM_SMTP_PASSWORD env var'ı ayarlayın)";
            return false;
        }
        reason = "";
        return true;
    }

    private bool TryMarkSent(string fingerprint)
    {
        var now = DateTime.UtcNow;
        if (_cooldown.TryGetValue(fingerprint, out var last) && now - last < CooldownPeriod)
            return false;

        _cooldown[fingerprint] = now;
        return true;
    }

    private static string BuildFingerprint(Exception? exception, string message)
    {
        var typeName = exception?.GetType().Name ?? "Unknown";
        var msg = (exception?.Message ?? message);
        return $"{typeName}:{(msg.Length > 100 ? msg[..100] : msg)}";
    }

    private static string BuildHtmlBody(string message, Exception? exception, NotificationContext? context)
    {
        var appVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "?";
        var timestamp  = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        var level      = context?.Level    ?? "Error";
        var screen     = context?.Screen   ?? "—";
        var userId     = context?.UserId   ?? "—";
        var userName   = context?.UserName ?? "—";
        var exType     = exception?.GetType().FullName ?? "—";
        var stackTrace = WebUtility.HtmlEncode(exception?.StackTrace ?? "—")
                         .Replace("\n", "<br/>");

        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family:Arial,sans-serif;font-size:13px;color:#333;max-width:800px;">
              <h2 style="color:#C62828;border-bottom:2px solid #C62828;padding-bottom:6px;">
                ⚠ Yönetim Finansal İşlem Takip Sistemi — Kritik Hata Bildirimi
              </h2>
              <table style="border-collapse:collapse;width:100%;margin-bottom:16px;">
                <tr style="background:#F5F5F5;"><td style="padding:5px 10px;width:190px;font-weight:bold;">Tarih</td><td style="padding:5px 10px;">{timestamp}</td></tr>
                <tr><td style="padding:5px 10px;font-weight:bold;">Makine Adı</td><td style="padding:5px 10px;">{WebUtility.HtmlEncode(Environment.MachineName)}</td></tr>
                <tr style="background:#F5F5F5;"><td style="padding:5px 10px;font-weight:bold;">Windows Kullanıcısı</td><td style="padding:5px 10px;">{WebUtility.HtmlEncode(Environment.UserName)}</td></tr>
                <tr><td style="padding:5px 10px;font-weight:bold;">Uygulama Sürümü</td><td style="padding:5px 10px;">{WebUtility.HtmlEncode(appVersion)}</td></tr>
                <tr style="background:#F5F5F5;"><td style="padding:5px 10px;font-weight:bold;">Hata Seviyesi</td><td style="padding:5px 10px;color:#C62828;font-weight:bold;">{WebUtility.HtmlEncode(level)}</td></tr>
                <tr><td style="padding:5px 10px;font-weight:bold;">Ekran / İşlem</td><td style="padding:5px 10px;">{WebUtility.HtmlEncode(screen)}</td></tr>
                <tr style="background:#F5F5F5;"><td style="padding:5px 10px;font-weight:bold;">Kullanıcı ID</td><td style="padding:5px 10px;">{WebUtility.HtmlEncode(userId)}</td></tr>
                <tr><td style="padding:5px 10px;font-weight:bold;">Kullanıcı Adı</td><td style="padding:5px 10px;">{WebUtility.HtmlEncode(userName)}</td></tr>
              </table>
              <h3 style="color:#444;">Hata Mesajı</h3>
              <p style="background:#FFF3E0;padding:10px;border-left:4px solid #E65C00;">{WebUtility.HtmlEncode(message)}</p>
              <h3 style="color:#444;">Exception Tipi</h3>
              <p><code>{WebUtility.HtmlEncode(exType)}</code></p>
              <h3 style="color:#444;">Stack Trace</h3>
              <pre style="background:#F5F5F5;padding:10px;font-size:11px;white-space:pre-wrap;">{stackTrace}</pre>
              <hr style="border:none;border-top:1px solid #DDD;margin-top:20px;"/>
              <p style="color:#999;font-size:11px;">Bu e-posta Yönetim Finansal İşlem Takip Sistemi tarafından otomatik gönderilmiştir.</p>
            </body>
            </html>
            """;
    }
}
