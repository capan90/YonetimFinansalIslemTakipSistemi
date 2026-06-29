using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.SystemLogs;

public class SystemLogDetailDto
{
    public Guid           Id                    { get; init; }
    public SystemLogLevel Level                 { get; init; }
    public string         LevelDisplay          { get; init; } = string.Empty;
    public string         Category              { get; init; } = string.Empty;
    public string         Message               { get; init; } = string.Empty;
    public string?        ExceptionType         { get; init; }
    public string?        StackTrace            { get; init; }
    public string?        InnerExceptionMessage { get; init; }
    public string?        Source                { get; init; }
    public Guid?          UserId                { get; init; }
    public string?        Username              { get; init; }
    public string         MachineName           { get; init; } = string.Empty;
    public string?        AppVersion            { get; init; }
    public bool           IsCritical            { get; init; }
    public bool           IsResolved            { get; init; }
    public DateTime?      ResolvedAt            { get; init; }
    public Guid?          ResolvedByUserId      { get; init; }
    public string?        ResolutionNote        { get; init; }
    public DateTime       CreatedAt             { get; init; }
}
