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
        // Öncelik: YONETIM_DB_CONNECTION env var > appsettings.json > hata
        var connectionString = Environment.GetEnvironmentVariable("YONETIM_DB_CONNECTION")
            ?? ReadConnectionStringFromConfig();

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Bağlantı dizesi bulunamadı.\n" +
                "Seçenek 1 — Ortam değişkeni ayarla:\n" +
                "  set YONETIM_DB_CONNECTION=Host=localhost;Port=5432;Database=yonetim_db;Username=postgres;Password=...\n" +
                "Seçenek 2 — UI projesindeki appsettings.json dosyasını düzenle ve 'dotnet ef' komutunu UI proje dizininden çalıştır.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static string? ReadConnectionStringFromConfig()
    {
        // dotnet ef komutu genellikle proje kök dizininden veya UI proje dizininden çalıştırılır.
        // Her iki konumda da appsettings.json aranır.
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        return config.GetConnectionString("DefaultConnection");
    }
}
