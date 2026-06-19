namespace YonetimFinansalIslemTakipSistemi.Application.Features.AuditLogs.Queries.GetAuditLogs;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string ActionDisplay { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string ComputerName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
