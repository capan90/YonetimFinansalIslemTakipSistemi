using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.SystemLogs;

public class SystemLogListItemDto
{
    public Guid           Id            { get; init; }
    public SystemLogLevel Level         { get; init; }
    public string         LevelDisplay  { get; init; } = string.Empty;
    public string         Category      { get; init; } = string.Empty;
    public string         Message       { get; init; } = string.Empty;
    public string?        Username      { get; init; }
    public string         MachineName   { get; init; } = string.Empty;
    public bool           IsCritical    { get; init; }
    public bool           IsResolved    { get; init; }
    public string         StatusDisplay { get; init; } = string.Empty;
    public DateTime       CreatedAt     { get; init; }
}
