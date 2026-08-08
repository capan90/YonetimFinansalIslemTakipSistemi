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

    /// <summary>
    /// Sprint 21: repository'ler IDbContextFactory alır. DB erişilebilirse
    /// gerçek factory döner; değilse null (test atlanır).
    ///
    /// Faz F2: sonda TEST BAŞINA DEĞİL, koleksiyon başına bir kez yapılır —
    /// çağıran <see cref="LiveDatabaseFixture"/>. Context üretimi de oradan
    /// geçer; ayrı bir TryCreateContext yolu kalmadı.
    /// </summary>
    public static IDbContextFactory<AppDbContext>? TryCreateFactory()
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

        return new SimpleDbContextFactory(cs);
    }
}

/// <summary>Test için minimal IDbContextFactory: her çağrıda taze context üretir (üretimdeki desenle aynı).</summary>
internal sealed class SimpleDbContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly DbContextOptions<AppDbContext> _options;

    public SimpleDbContextFactory(string connectionString)
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public AppDbContext CreateDbContext() => new(_options);
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
