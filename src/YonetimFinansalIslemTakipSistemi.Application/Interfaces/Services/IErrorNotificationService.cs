namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Kritik hata durumlarında bildirim gönderim sözleşmesi.
/// Implementasyon: NullErrorNotificationService (varsayılan) veya SmtpErrorNotificationService.
/// </summary>
public interface IErrorNotificationService
{
    Task NotifyAsync(string message, Exception? exception = null, NotificationContext? context = null);

    /// <summary>
    /// SMTP yapılandırmasını doğrulamak için test maili gönderir.
    /// Başarı: (true, null). Hata: (false, açıklama). Stack trace verilmez; şifre gösterilmez.
    /// </summary>
    Task<(bool Success, string? Error)> SendTestAsync();
}
