namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Kargo bilgilendirme mailini gönderme sözleşmesi.
/// Mevcut SmtpNotificationOptions SMTP altyapısını kullanır; ayrı config gerektirmez.
/// </summary>
public interface ICargoMailSenderService
{
    Task<(bool Success, string? Error)> SendAsync(
        string  to,
        string? cc,
        string  subject,
        string  body);
}
