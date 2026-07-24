using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);

    /// <summary>Toplu yazım — tek SaveChanges (import senaryoları için).</summary>
    Task AddRangeAsync(IReadOnlyList<AuditLog> logs);
    Task<List<AuditLog>> GetFilteredAsync(Guid? userId, DateTime? from, DateTime? to, AuditAction? action);
}
