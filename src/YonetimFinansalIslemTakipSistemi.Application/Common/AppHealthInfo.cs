namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// Uygulama sağlık durumu özeti.
/// Sprint 3'te veri yapısı hazırlandı; UI ve servis implementasyonu sonraki sprintte eklenecek.
/// </summary>
public sealed record AppHealthInfo
{
    public string   AppVersion      { get; init; } = string.Empty;
    public string   MachineName     { get; init; } = string.Empty;
    public string   DatabaseName    { get; init; } = string.Empty;
    public string   DataSource      { get; init; } = string.Empty;
    public string   LastMigration   { get; init; } = string.Empty;
    public string   LogDirectory    { get; init; } = string.Empty;
    public string   BackupDirectory { get; init; } = string.Empty;
    public DateTime CheckedAt       { get; init; }
}
