using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

public class HealthCheckService : IHealthCheckService
{
    private readonly AppDbContext _dbContext;
    private readonly HealthCheckOptions _options;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(
        AppDbContext dbContext,
        HealthCheckOptions options,
        ILogger<HealthCheckService> logger)
    {
        _dbContext = dbContext;
        _options   = options;
        _logger    = logger;
    }

    public async Task<AppHealthInfo> GetHealthAsync()
    {
        var checkedAt  = DateTime.Now;
        var appVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "?";

        // ── Veritabanı ──────────────────────────────────────────────────────

        bool   dbCanConnect = false;
        string lastMigration = "Bilinmiyor";
        int    pendingCount  = 0;

        try
        {
            dbCanConnect = await _dbContext.Database.CanConnectAsync();
            if (dbCanConnect)
            {
                var applied = (await _dbContext.Database.GetAppliedMigrationsAsync()).ToList();
                lastMigration = applied.LastOrDefault() ?? "Yok";
                pendingCount  = (await _dbContext.Database.GetPendingMigrationsAsync()).Count();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check: veritabanı sorgusu başarısız");
            dbCanConnect  = false;
            lastMigration = "Erişilemiyor";
        }

        var connMap    = ParseConnectionString(_options.ConnectionString);
        var dbName     = connMap.GetValueOrDefault("database", "");
        var dataSource = BuildDataSource(connMap);

        // ── Log klasörü ─────────────────────────────────────────────────────

        var logDir       = _options.LogDirectory;
        var logDirExists = !string.IsNullOrEmpty(logDir) && Directory.Exists(logDir);
        var latestLog    = logDirExists ? GetLatestFileName(logDir, "*.log") : "";

        // ── Backup klasörü ───────────────────────────────────────────────────

        var backupDir       = _options.BackupDirectory;
        var backupDirExists = !string.IsNullOrEmpty(backupDir) && Directory.Exists(backupDir);
        var latestBackup    = backupDirExists ? GetLatestFileName(backupDir, "*.backup") : "";

        // ── Güncelleme / version.json ────────────────────────────────────────

        var publishPath     = _options.UpdatePublishPath;
        var versionJsonPath = string.IsNullOrEmpty(publishPath)
            ? ""
            : Path.Combine(publishPath, "version.json");

        var versionJsonExists     = !string.IsNullOrEmpty(versionJsonPath) && File.Exists(versionJsonPath);
        var latestPublishedVersion = "";

        if (versionJsonExists)
        {
            try
            {
                var json = await File.ReadAllTextAsync(versionJsonPath);
                using var doc = JsonDocument.Parse(json);
                latestPublishedVersion = doc.RootElement.GetProperty("version").GetString() ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Health check: version.json okunamadı: {Path}", versionJsonPath);
                latestPublishedVersion = "Okunamadı";
            }
        }

        // ── Sonuç ────────────────────────────────────────────────────────────

        var info = new AppHealthInfo
        {
            AppVersion            = appVersion,
            MachineName           = Environment.MachineName,
            WindowsUserName       = Environment.UserName,
            AppEnvironment        = _options.AppEnvironment,
            DatabaseCanConnect    = dbCanConnect,
            DatabaseName          = dbName,
            DataSource            = dataSource,
            LastMigration         = lastMigration,
            PendingMigrationCount = pendingCount,
            LogDirectory          = logDir,
            LogDirectoryExists    = logDirExists,
            LatestLogFile         = latestLog,
            BackupDirectory       = backupDir,
            BackupDirectoryExists = backupDirExists,
            LatestBackupFile      = latestBackup,
            UpdatePublishPath     = publishPath,
            VersionJsonPath       = versionJsonPath,
            VersionJsonExists     = versionJsonExists,
            LatestPublishedVersion   = latestPublishedVersion,
            NotificationsEnabled     = _options.NotificationsEnabled,
            NotificationProvider     = _options.NotificationProvider,
            NotificationToConfigured = _options.NotificationToConfigured,
            NotificationSmtpHost    = _options.NotificationSmtpHost,
            CheckedAt               = checkedAt
        };

        return info with { OverallStatus = CalculateOverallStatus(info) };
    }

    // ── Yardımcı metodlar ────────────────────────────────────────────────────

    private static HealthStatus CalculateOverallStatus(AppHealthInfo info)
    {
        // Hata: DB bağlantısı yok — uygulama işlevsiz
        if (!info.DatabaseCanConnect) return HealthStatus.Error;

        // Uyarı: bekleyen migration, backup yok, version.json yok, log klasörü yok
        if (info.PendingMigrationCount > 0)                                             return HealthStatus.Warning;
        if (!info.BackupDirectoryExists || string.IsNullOrEmpty(info.LatestBackupFile)) return HealthStatus.Warning;
        if (!info.VersionJsonExists)                                                     return HealthStatus.Warning;
        if (!info.LogDirectoryExists)                                                    return HealthStatus.Warning;

        return HealthStatus.Ok;
    }

    private static Dictionary<string, string> ParseConnectionString(string connStr)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(connStr)) return map;

        foreach (var part in connStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            if (eq > 0)
                map[part[..eq].Trim()] = part[(eq + 1)..].Trim();
        }
        return map;
    }

    private static string BuildDataSource(Dictionary<string, string> map)
    {
        var host = map.GetValueOrDefault("host", "localhost");
        var port = map.GetValueOrDefault("port", "5432");
        return $"{host}:{port}";
    }

    private static string GetLatestFileName(string directory, string pattern)
    {
        try
        {
            return Directory.GetFiles(directory, pattern)
                .OrderByDescending(File.GetLastWriteTime)
                .Select(Path.GetFileName)
                .FirstOrDefault() ?? "";
        }
        catch
        {
            return "";
        }
    }
}
