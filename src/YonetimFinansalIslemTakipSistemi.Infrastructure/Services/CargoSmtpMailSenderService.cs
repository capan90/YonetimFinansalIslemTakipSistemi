using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// Kargo bilgilendirme maili için SMTP gönderici.
/// SMTP ayarları artık appsettings değil, application_settings tablosundan okunur.
/// </summary>
public class CargoSmtpMailSenderService : ICargoMailSenderService
{
    private readonly IMailSettingsService                _mailSettings;
    private readonly ILogger<CargoSmtpMailSenderService> _logger;

    public CargoSmtpMailSenderService(
        IMailSettingsService                 mailSettings,
        ILogger<CargoSmtpMailSenderService>  logger)
    {
        _mailSettings = mailSettings;
        _logger       = logger;
    }

    public async Task<(bool Success, string? Error)> SendAsync(
        IReadOnlyCollection<string> to, IReadOnlyCollection<string> cc, string subject, string body)
    {
        if (to.Count == 0)
            return (false, "En az bir alıcı adresi gereklidir.");

        var settings = await _mailSettings.GetAsync();

        if (settings is null || string.IsNullOrWhiteSpace(settings.SmtpHost))
            return (false, "Mail ayarları yapılandırılmamış. Ayarlar → Mail Ayarları bölümünden SMTP bilgilerini girin.");

        if (settings.PasswordDecryptFailed)
            return (false, "Mail şifresi çözümlenemedi. Mail ayarlarını yeniden kaydedin.");

        if (settings.SmtpPort <= 0)
            return (false, "SMTP port numarası geçersiz. Mail ayarlarını kontrol edin.");

        if (string.IsNullOrWhiteSpace(settings.SenderEmail))
            return (false, "Gönderen e-posta adresi ayarlanmamış. Ayarlar → Mail Ayarları bölümüne bakın.");

        try
        {
            using var mail = new MailMessage();
            mail.From    = new MailAddress(settings.SenderEmail, settings.SenderName);
            // Çoklu alıcı: her adres ayrı MailAddress olarak eklenir. Tek string'i
            // noktalı virgülle geçmek bazı SMTP sunucularında sessizce tek alıcıya düşüyordu.
            foreach (var address in to) mail.To.Add(new MailAddress(address));
            foreach (var address in cc) mail.CC.Add(new MailAddress(address));
            mail.Subject    = subject;
            mail.Body       = body;
            mail.IsBodyHtml = false;

            using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
            {
                EnableSsl             = settings.EnableSsl,
                DeliveryMethod        = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout               = 15_000
            };

            if (!string.IsNullOrWhiteSpace(settings.Username))
                client.Credentials = new NetworkCredential(settings.Username, settings.Password);

            var toLog = string.Join("; ", to);
            var ccLog = cc.Count > 0 ? string.Join("; ", cc) : "-";

            _logger.LogInformation(
                "SMTP gönderim başlatılıyor → Host:{Host} Port:{Port} SSL:{Ssl} Gönderici:{From} Alıcı:{To} CC:{Cc}",
                settings.SmtpHost, settings.SmtpPort, settings.EnableSsl, settings.SenderEmail, toLog, ccLog);

            await client.SendMailAsync(mail);
            _logger.LogInformation("Kargo bildirim maili gönderildi → {To} (CC: {Cc})", toLog, ccLog);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kargo bildirim maili gönderilemedi → {ExType}: {ExMsg} | Alıcı:{To}",
                ex.GetType().Name, ex.Message, string.Join("; ", to));
            return (false, ex.Message);
        }
    }
}
