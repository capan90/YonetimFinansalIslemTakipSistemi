using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.SystemLogs;

public class SystemLogSearchQuery
{
    public DateTime?      DateFrom   { get; init; }
    public DateTime?      DateTo     { get; init; }
    public SystemLogLevel? Level     { get; init; }
    public string?        Category   { get; init; }
    public bool?          IsResolved { get; init; }
    public string?        SearchText { get; init; }
    public int            Page       { get; init; } = 1;
    public int            PageSize   { get; init; } = 50;
}
