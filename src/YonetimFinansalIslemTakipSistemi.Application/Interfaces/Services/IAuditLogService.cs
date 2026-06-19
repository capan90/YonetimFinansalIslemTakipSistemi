using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

public interface IAuditLogService
{
    Task WriteAsync(AuditAction action, Guid userId, string userName,
                   string entityType, Guid? entityId,
                   string? oldValues = null, string? newValues = null);
}
