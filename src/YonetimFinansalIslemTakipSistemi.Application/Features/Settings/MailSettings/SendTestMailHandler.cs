using System.Net;
using System.Net.Mail;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Settings.MailSettings;

public class SendTestMailHandler
{
    private readonly IUserContext _userContext;

    public SendTestMailHandler(IUserContext userContext)
    {
        _userContext = userContext;
    }

    /// <summary>
    /// Form'daki geçici ayarlarla test maili gönderir — kaydetmeden önce de çalışır.
    /// </summary>
    public async Task<OperationResult<bool>> HandleAsync(MailSettingsDto settings, string testRecipient)
    {
        if (!_userContext.HasPermission(PermissionType.CanManageMailSettings))
            return OperationResult<bool>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        if (string.IsNullOrWhiteSpace(settings.SmtpHost))
            return OperationResult<bool>.Fail("SMTP sunucusu girilmemiş.");

        if (string.IsNullOrWhiteSpace(settings.SenderEmail))
            return OperationResult<bool>.Fail("Gönderen e-posta adresi girilmemiş.");

        if (string.IsNullOrWhiteSpace(testRecipient))
            return OperationResult<bool>.Fail("Test mail alıcısı girilmemiş.");

        try
        {
            using var mail = new MailMessage();
            mail.From    = new MailAddress(settings.SenderEmail, settings.SenderName);
            mail.To.Add(testRecipient);
            mail.Subject = "Test Maili — Yönetim Sistemi";
            mail.Body    = "Bu bir test mailidir. SMTP ayarları başarıyla doğrulandı.";

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
            return OperationResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            return OperationResult<bool>.Fail($"Test maili gönderilemedi: {ex.Message}");
        }
    }
}
