using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

// YALNIZCA GELİŞTİRME / MİGRATION AMACI İÇİN.
// Bu factory, EF Core CLI araçlarının (dotnet ef) WPF startup host olmadan
// AppDbContext'i bulabilmesi için tasarlanmıştır.
// Üretim ortamında çağrılmaz; bağlantı yönetimi App.xaml.cs composition root'unda yapılır.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Öncelik: YONETIM_DB_CONNECTION env var > appsettings.{Environment}.json > appsettings.json > hata
        var connectionString = Environment.GetEnvironmentVariable("YONETIM_DB_CONNECTION")
            ?? ReadConnectionStringFromConfig();

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Bağlantı dizesi bulunamadı.\n" +
                "Seçenek 1 — Ortam değişkeni ayarla:\n" +
                "  set YONETIM_DB_CONNECTION=Host=localhost;Port=5432;Database=yonetim_db;Username=postgres;Password=...\n" +
                "Seçenek 2 — Komutu proje kökünden veya UI proje dizininden çalıştır; " +
                "src/YonetimFinansalIslemTakipSistemi.UI/appsettings.json okunacaktır.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static string? ReadConnectionStringFromConfig()
    {
        var configPath = FindAppsettingsPath();
        if (configPath is null) return null;

        var basePath = Path.GetDirectoryName(configPath)!;

        // App.xaml.cs ile aynı ortam mantığı: base appsettings.json'daki AppEnvironment yedek görevi görür.
        var baseConfig = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var environment = ResolveEnvironmentName(baseConfig["AppEnvironment"]);

        // Katmanlı yükleme: appsettings.json → appsettings.{Environment}.json → ortam değişkenleri.
        // Env var yoksa varsayılan Development'tır; dotnet ef bu yüzden yanlışlıkla canlı DB'ye gitmez.
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        return config.GetConnectionString("DefaultConnection");
    }

    /// <summary>
    /// Aktif ortam adını çözer. Öncelik sırası:
    ///   YONETIM_ENVIRONMENT > DOTNET_ENVIRONMENT > ASPNETCORE_ENVIRONMENT > config AppEnvironment > "Development"
    /// </summary>
    private static string ResolveEnvironmentName(string? configFallback)
    {
        return FirstNonEmpty(Environment.GetEnvironmentVariable("YONETIM_ENVIRONMENT"))
            ?? FirstNonEmpty(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"))
            ?? FirstNonEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
            ?? FirstNonEmpty(configFallback)
            ?? "Development";
    }

    private static string? FirstNonEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>
    /// appsettings.json için şu konumları sırayla arar:
    /// 1. Geçerli dizin (dotnet ef UI proje dizininden çalıştırıldığında)
    /// 2. Geçerli dizinden yukarı doğru: src/YonetimFinansalIslemTakipSistemi.UI/appsettings.json
    ///    (proje kökünden, Infrastructure dizininden veya herhangi bir ata dizininden çalıştırıldığında)
    /// </summary>
    private static string? FindAppsettingsPath()
    {
        const string fileName = "appsettings.json";
        var uiSubPath = Path.Combine("src", "YonetimFinansalIslemTakipSistemi.UI", fileName);
        var cwd = Directory.GetCurrentDirectory();

        // Durum 1: dotnet ef UI proje dizininden çalıştırılmış — appsettings.json burada
        var direct = Path.Combine(cwd, fileName);
        if (File.Exists(direct)) return direct;

        // Durum 2: Proje kökünden veya herhangi bir alt dizinden çalıştırılmış.
        // Ata dizinlerde src/UI/appsettings.json kombinasyonunu ara.
        var dir = new DirectoryInfo(cwd);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, uiSubPath);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        return null;
    }
}
