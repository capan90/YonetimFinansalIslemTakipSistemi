namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// Uygulama sağlık durumu özeti — Sistem Sağlığı ekranına besler.
/// </summary>
public sealed record AppHealthInfo
{
    // ── Uygulama ────────────────────────────────────────────────────────────
    public string AppVersion       { get; init; } = string.Empty;
    public string MachineName      { get; init; } = string.Empty;
    public string WindowsUserName  { get; init; } = string.Empty;
    public string AppEnvironment   { get; init; } = string.Empty;

    // ── Veritabanı ──────────────────────────────────────────────────────────
    public bool   DatabaseCanConnect    { get; init; }
    public string DatabaseName          { get; init; } = string.Empty;
    public string DataSource            { get; init; } = string.Empty;
    public string LastMigration         { get; init; } = string.Empty;
    public int    PendingMigrationCount { get; init; }

    // ── Loglar ──────────────────────────────────────────────────────────────
    public string LogDirectory    { get; init; } = string.Empty;
    public bool   LogDirectoryExists { get; init; }
    public string LatestLogFile   { get; init; } = string.Empty;

    // ── Backup ──────────────────────────────────────────────────────────────
    public string BackupDirectory      { get; init; } = string.Empty;
    public bool   BackupDirectoryExists { get; init; }
    public string LatestBackupFile     { get; init; } = string.Empty;

    // ── Güncelleme ──────────────────────────────────────────────────────────
    public string UpdatePublishPath      { get; init; } = string.Empty;
    public string VersionJsonPath        { get; init; } = string.Empty;
    public bool   VersionJsonExists      { get; init; }
    public string LatestPublishedVersion { get; init; } = string.Empty;

    // ── Özet ────────────────────────────────────────────────────────────────
    public HealthStatus OverallStatus { get; init; }
    public DateTime     CheckedAt     { get; init; }
}
