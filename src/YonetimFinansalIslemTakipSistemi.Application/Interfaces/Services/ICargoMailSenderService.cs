namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Kargo bilgilendirme mailini gönderme sözleşmesi.
/// SMTP ayarları IMailSettingsService üzerinden DB'den okunur; ayrı config gerektirmez.
/// </summary>
public interface ICargoMailSenderService
{
    /// <summary>
    /// Adresler normalize/doğrulanmış olarak gelmelidir (EmailAddressHelper.Parse).
    /// Alıcı listesi birden fazla adres içerebilir; boş olamaz. CC boş olabilir.
    /// </summary>
    Task<(bool Success, string? Error)> SendAsync(
        IReadOnlyCollection<string> to,
        IReadOnlyCollection<string> cc,
        string                      subject,
        string                      body);
}
