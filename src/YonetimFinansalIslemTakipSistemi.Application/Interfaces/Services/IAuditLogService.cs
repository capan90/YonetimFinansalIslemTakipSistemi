using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

public interface IAuditLogService
{
    Task WriteAsync(AuditAction action, Guid userId, string userName,
                   string entityType, Guid? entityId,
                   string? oldValues = null, string? newValues = null);

    /// <summary>
    /// Toplu işlemler (Excel import vb.) için: tüm kayıtlar TEK SaveChanges ile yazılır.
    /// Binlerce satırlık importta kayıt başına WriteAsync çağırmak UI'ı dondurur —
    /// bu metot zorunludur. WriteAsync gibi hataya dayanıklıdır (audit hatası akışı bloke etmez).
    /// </summary>
    Task WriteRangeAsync(IReadOnlyList<AuditEntry> entries);
}

/// <summary>Toplu audit yazımı için tek kayıt tanımı.</summary>
public sealed record AuditEntry(
    AuditAction Action,
    Guid UserId,
    string UserName,
    string EntityType,
    Guid? EntityId,
    string? OldValues = null,
    string? NewValues = null);
