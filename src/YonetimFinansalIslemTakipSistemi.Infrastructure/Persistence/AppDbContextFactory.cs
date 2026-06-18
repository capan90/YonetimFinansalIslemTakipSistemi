using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

// YALNIZCA GELİŞTİRME / MİGRATION AMACI İÇİN.
// Bu factory, EF Core CLI araçlarının (dotnet ef) WPF startup host olmadan
// AppDbContext'i bulabilmesi için tasarlanmıştır.
// Üretim ortamında çağrılmaz; bağlantı yönetimi App.xaml.cs composition root'unda yapılır.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Önce ortam değişkeninden oku; ayarlanmamışsa geliştirme varsayılanını kullan.
        var connectionString = Environment.GetEnvironmentVariable("YONETIM_DB_CONNECTION")
            ?? "Host=localhost;Database=yonetim_db;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
