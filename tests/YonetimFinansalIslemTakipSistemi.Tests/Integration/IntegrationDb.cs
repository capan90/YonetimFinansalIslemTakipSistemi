using Microsoft.EntityFrameworkCore;
using Npgsql;
using YonetimFinansalIslemTakipSistemi.Application.Features.SystemLogs;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Tests.Integration;

/// <summary>
/// Gerçek dev PostgreSQL bağlantısı için yardımcı. Bağlantı dizesi önceliği:
/// YONETIM_DB_CONNECTION env var > src/UI/appsettings.Development.json.
/// DB erişilemiyorsa testler sessizce atlanır (CI/offline ortam kırılmasın).
/// </summary>
internal static class IntegrationDb
{
    public static string? ResolveConnectionString()
    {
        var env = Environment.GetEnvironmentVariable("YONETIM_DB_CONNECTION");
        if (!string.IsNullOrWhiteSpace(env)) return env;

        // Test çalışma dizininden yukarı çıkıp UI appsettings.Development.json'ı bul
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName,
                "src", "YonetimFinansalIslemTakipSistemi.UI", "appsettings.Development.json");
            if (File.Exists(candidate))
            {
                var json = File.ReadAllText(candidate);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                return doc.RootElement
                    .GetProperty("ConnectionStrings")
                    .GetProperty("DefaultConnection")
                    .GetString();
            }
            dir = dir.Parent;
        }
        return null;
    }

    /// <summary>DB erişilebilirse yeni AppDbContext döner; değilse null (test atlanır).</summary>
    public static AppDbContext? TryCreateContext()
    {
        var cs = ResolveConnectionString();
        if (cs is null) return null;

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(cs) { Timeout = 3 };
            using var probe = new NpgsqlConnection(builder.ConnectionString);
            probe.Open();
        }
        catch
        {
            return null; // DB kapalı/erişilemez — integration testler atlanır
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs)
            .Options;
        return new AppDbContext(options);
    }
}

/// <summary>Integration testlerde repository bağımlılığı için sessiz system log.</summary>
internal sealed class NoOpSystemLogService : ISystemLogService
{
    public Task LogInfoAsync(string category, string message, string? source = null) => Task.CompletedTask;
    public Task LogWarningAsync(string category, string message, string? source = null) => Task.CompletedTask;
    public Task LogErrorAsync(string category, string message, Exception? exception = null, string? source = null) => Task.CompletedTask;
    public Task LogCriticalAsync(string category, string message, Exception? exception = null, string? source = null) => Task.CompletedTask;
    public Task<PagedSystemLogResultDto> SearchAsync(SystemLogSearchQuery query) => Task.FromResult(new PagedSystemLogResultDto());
    public Task<SystemLogDetailDto?> GetByIdAsync(Guid id) => Task.FromResult<SystemLogDetailDto?>(null);
    public Task MarkResolvedAsync(Guid id, Guid resolvedByUserId, string? note) => Task.CompletedTask;
}
