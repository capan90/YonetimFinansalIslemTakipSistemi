using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// Hata bildirimi stub'u — hiçbir şey göndermez.
/// SmtpErrorNotificationService kayıtlı değilse veya testlerde kullanılır.
/// </summary>
public class NullErrorNotificationService : IErrorNotificationService
{
    public Task NotifyAsync(string message, Exception? exception = null, NotificationContext? context = null)
        => Task.CompletedTask;

    public Task<(bool Success, string? Error)> SendTestAsync()
        => Task.FromResult<(bool, string?)>((false, "Mail bildirimi devre dışı (NullErrorNotificationService aktif)"));
}
