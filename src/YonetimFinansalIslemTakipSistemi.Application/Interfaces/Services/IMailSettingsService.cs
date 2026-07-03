using YonetimFinansalIslemTakipSistemi.Application.Features.Settings.MailSettings;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Mail SMTP ayarlarını veritabanından okur.
/// Singleton olarak kaydedilir; her çağrıda DB'den taze veri getirir.
/// </summary>
public interface IMailSettingsService
{
    /// <summary>
    /// Mevcut aktif kullanıcının mail ayarlarını döner.
    /// Aktif kullanıcının ayarı yoksa genel (global) SMTP ayarlarına fallback yapar.
    /// </summary>
    Task<MailSettingsDto?> GetAsync();

    /// <summary>
    /// Yalnızca genel (sistem bazlı) SMTP ayarlarını döner.
    /// </summary>
    Task<MailSettingsDto?> GetGlobalAsync();

    /// <summary>
    /// Yalnızca belirtilen kullanıcıya özel SMTP ayarlarını döner. Fallback yapmaz.
    /// </summary>
    Task<MailSettingsDto?> GetPersonalOnlyAsync(Guid userId);
}
