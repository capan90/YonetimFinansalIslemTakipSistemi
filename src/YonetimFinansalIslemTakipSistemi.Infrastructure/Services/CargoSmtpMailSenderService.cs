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
        string to, string? cc, string subject, string body)
    {
        var settings = await _mailSettings.GetAsync();

        if (settings is null || string.IsNullOrWhiteSpace(settings.SmtpHost))
            return (false, "Mail ayarları yapılandırılmamış. Ayarlar → Mail Ayarları bölümünden SMTP bilgilerini girin.");

        if (string.IsNullOrWhiteSpace(settings.SenderEmail))
            return (false, "Gönderen e-posta adresi ayarlanmamış. Ayarlar → Mail Ayarları bölümüne bakın.");

        try
        {
            using var mail = new MailMessage();
            mail.From    = new MailAddress(settings.SenderEmail, settings.SenderName);
            mail.To.Add(to);
            if (!string.IsNullOrWhiteSpace(cc)) mail.CC.Add(cc);
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

            await client.SendMailAsync(mail);
            _logger.LogInformation("Kargo bildirim maili gönderildi → {To}", to);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kargo bildirim maili gönderilemedi → {To}", to);
            return (false, ex.Message);
        }
    }
}
