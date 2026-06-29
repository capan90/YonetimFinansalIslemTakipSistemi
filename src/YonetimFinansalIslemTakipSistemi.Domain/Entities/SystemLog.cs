using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Domain.Entities;

// BaseEntity'den türetilmez — system log'un kendi audit trail'i olmaz
public class SystemLog
{
    public Guid           Id                    { get; set; }
    public SystemLogLevel Level                 { get; set; }
    public string         Category              { get; set; } = string.Empty;
    public string         Message               { get; set; } = string.Empty;
    public string?        ExceptionType         { get; set; }
    public string?        StackTrace            { get; set; }
    public string?        InnerExceptionMessage { get; set; }
    public string?        Source                { get; set; }
    public Guid?          UserId                { get; set; }
    public string?        Username              { get; set; }
    public string         MachineName           { get; set; } = string.Empty;
    public string?        AppVersion            { get; set; }
    public bool           IsCritical            { get; set; }
    public bool           IsResolved            { get; set; }
    public DateTime?      ResolvedAt            { get; set; }
    public Guid?          ResolvedByUserId      { get; set; }
    public string?        ResolutionNote        { get; set; }
    public DateTime       CreatedAt             { get; set; }
}
