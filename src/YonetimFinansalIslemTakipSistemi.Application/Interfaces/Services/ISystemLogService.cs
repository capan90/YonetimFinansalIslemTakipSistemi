using YonetimFinansalIslemTakipSistemi.Application.Features.SystemLogs;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Uygulama düzeyinde sistem olayı ve hata kaydı.
/// Audit Log ile karıştırılmaz: bu teknik/operasyonel olaylar içindir.
/// </summary>
public interface ISystemLogService
{
    Task LogInfoAsync(string category, string message, string? source = null);
    Task LogWarningAsync(string category, string message, string? source = null);
    Task LogErrorAsync(string category, string message, Exception? exception = null, string? source = null);

    /// <summary>
    /// Critical log yazar ve IErrorNotificationService üzerinden mail gönderir.
    /// systemLogId: mail içeriğine eklenir; null gelirse DB kaydından otomatik atanır.
    /// </summary>
    Task LogCriticalAsync(string category, string message, Exception? exception = null, string? source = null);

    Task<PagedSystemLogResultDto>  SearchAsync(SystemLogSearchQuery query);
    Task<SystemLogDetailDto?>      GetByIdAsync(Guid id);
    Task                           MarkResolvedAsync(Guid id, Guid resolvedByUserId, string? note);
}
