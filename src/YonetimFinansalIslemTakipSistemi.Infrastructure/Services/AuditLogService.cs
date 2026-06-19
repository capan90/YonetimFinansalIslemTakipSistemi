using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;

    public AuditLogService(IAuditLogRepository repository) => _repository = repository;

    public async Task WriteAsync(AuditAction action, Guid userId, string userName,
                                string entityType, Guid? entityId,
                                string? oldValues = null, string? newValues = null)
    {
        var log = new AuditLog
        {
            Id           = Guid.NewGuid(),
            UserId       = userId,
            UserName     = userName,
            Action       = action,
            EntityType   = entityType,
            EntityId     = entityId,
            OldValues    = oldValues,
            NewValues    = newValues,
            ComputerName = Environment.MachineName,
            Timestamp    = DateTime.UtcNow
        };

        await _repository.AddAsync(log);
    }
}
