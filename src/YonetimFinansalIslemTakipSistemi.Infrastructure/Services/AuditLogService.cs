using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;
    private readonly ISystemLogService   _systemLog;

    public AuditLogService(IAuditLogRepository repository, ISystemLogService systemLog)
    {
        _repository = repository;
        _systemLog  = systemLog;
    }

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

        // Audit ve asıl mutasyon ayrı commit'lerdir: mutasyon DB'ye yazıldıktan sonra
        // audit hatasının yukarı taşınması, kullanıcının işlemi başarısız sanıp
        // tekrar denemesine (mükerrer kayıt) yol açar. Bu yüzden audit hatası ana
        // işlemi asla bloke etmez; kayıp audit System Log'a Error olarak düşer.
        // (SystemLogService kendi DB hatasında Serilog dosyasına düşer — döngü riski yok.)
        try
        {
            await _repository.AddAsync(log);
        }
        catch (Exception ex)
        {
            await _systemLog.LogErrorAsync("Audit",
                $"Audit kaydı yazılamadı: {action} | {entityType} | Kullanıcı: {userName}",
                ex, source: nameof(AuditLogService));
        }
    }
}
