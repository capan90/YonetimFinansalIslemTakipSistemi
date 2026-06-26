using YonetimFinansalIslemTakipSistemi.Application.Features.Settings.MailSettings;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Mail SMTP ayarlarını veritabanından okur.
/// Singleton olarak kaydedilir; her çağrıda DB'den taze veri getirir.
/// </summary>
public interface IMailSettingsService
{
    /// <summary>
    /// Mevcut mail ayarlarını döner.
    /// Ayarlar hiç girilmemişse null döner.
    /// </summary>
    Task<MailSettingsDto?> GetAsync();
}
