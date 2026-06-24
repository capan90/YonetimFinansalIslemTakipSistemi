namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Kritik hata durumlarında bildirim gönderim sözleşmesi.
/// Implementasyon: NullErrorNotificationService (varsayılan) veya SmtpErrorNotificationService.
/// </summary>
public interface IErrorNotificationService
{
    Task NotifyAsync(string message, Exception? exception = null, NotificationContext? context = null);
}
