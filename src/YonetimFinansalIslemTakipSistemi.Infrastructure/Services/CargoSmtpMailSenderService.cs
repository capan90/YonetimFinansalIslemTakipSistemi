using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// Kargo bilgilendirme maili için SMTP gönderici.
/// Mevcut ErrorNotifications SMTP altyapısını kullanır — ayrı sunucu/kimlik yapılandırması gerekmez.
/// Gönderici adresi: CargoNotificationOptions.FromEmail → SmtpNotificationOptions.From → SmtpUsername öncelik sırası.
/// </summary>
public class CargoSmtpMailSenderService : ICargoMailSenderService
{
    private readonly SmtpNotificationOptions              _smtp;
    private readonly CargoNotificationOptions             _cargo;
    private readonly ILogger<CargoSmtpMailSenderService>  _logger;

    public CargoSmtpMailSenderService(
        SmtpNotificationOptions              smtp,
        CargoNotificationOptions             cargo,
        ILogger<CargoSmtpMailSenderService>  logger)
    {
        _smtp   = smtp;
        _cargo  = cargo;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error)> SendAsync(
        string to, string? cc, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(_smtp.SmtpHost))
            return (false, "SMTP sunucusu yapılandırılmamış (appsettings → ErrorNotifications:Smtp:Host).");

        // Gönderici önceliği: cargo from > smtp from > smtp username
        var fromAddr = !string.IsNullOrWhiteSpace(_cargo.FromEmail) ? _cargo.FromEmail
                     : !string.IsNullOrWhiteSpace(_smtp.From)        ? _smtp.From
                     : _smtp.SmtpUsername;

        if (string.IsNullOrWhiteSpace(fromAddr))
            return (false, "Gönderici e-posta adresi yapılandırılmamış (CargoNotifications:FromEmail).");

        try
        {
            using var mail = new MailMessage();
            mail.From    = new MailAddress(fromAddr);
            mail.To.Add(to);
            if (!string.IsNullOrWhiteSpace(cc)) mail.CC.Add(cc);
            mail.Subject    = subject;
            mail.Body       = body;
            mail.IsBodyHtml = false;

            using var client = new SmtpClient(_smtp.SmtpHost, _smtp.SmtpPort)
            {
                EnableSsl             = _smtp.SmtpEnableSsl,
                DeliveryMethod        = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout               = 15_000
            };

            if (!string.IsNullOrWhiteSpace(_smtp.SmtpUsername))
                client.Credentials = new NetworkCredential(_smtp.SmtpUsername, _smtp.SmtpPassword);

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
