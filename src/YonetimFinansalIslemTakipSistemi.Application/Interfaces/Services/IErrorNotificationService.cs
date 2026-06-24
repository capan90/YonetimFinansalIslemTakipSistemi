namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Kritik hata durumlarında bildirim gönderim sözleşmesi.
/// Mevcut implementasyon stub'dur; sonraki sprintte SMTP ile gerçek gönderim sağlanacak.
/// </summary>
public interface IErrorNotificationService
{
    Task NotifyAsync(string message, Exception? exception = null);
}
