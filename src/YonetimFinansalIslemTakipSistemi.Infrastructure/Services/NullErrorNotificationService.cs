using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// Hata bildirimi stub'u — hiçbir şey göndermez.
/// Sonraki sprintte SmtpErrorNotificationService ile değiştirilecek.
/// appsettings.json: ErrorNotifications:Enabled = false iken bu sınıf aktiftir.
/// </summary>
public class NullErrorNotificationService : IErrorNotificationService
{
    public Task NotifyAsync(string message, Exception? exception = null)
        => Task.CompletedTask;
}
