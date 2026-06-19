using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure;

/// <summary>
/// Infrastructure servislerini DI container'a kaydeden extension.
/// UI katmanı Npgsql detaylarını bilmez; sadece bu metodu çağırır.
/// </summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        // QuestPDF Community lisansı — bu uygulama iç kullanım aracıdır, ticari ürün değildir.
        // Şirket geliri Community lisans eşiğini ($1M USD) aşarsa Professional/Enterprise lisansa geçilmeli.
        QuestPDF.Settings.License = LicenseType.Community;

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ICashTransactionRepository, CashTransactionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUserPermissionRepository, UserPermissionRepository>();
        services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();

        services.AddScoped<IAuthenticationService, DatabaseAuthenticationService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IReportExportService, ReportExportService>();

        // [DEV-ONLY] Geliştirme ortamı seed servisi
        services.AddScoped<IDevDataSeeder, DevDataSeeder>();

        return services;
    }
}
