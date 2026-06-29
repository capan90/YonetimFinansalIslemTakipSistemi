using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using YonetimFinansalIslemTakipSistemi.Application.Features.Analysis.Queries.GetDashboard;
using YonetimFinansalIslemTakipSistemi.Application.Common;
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
        services.AddScoped<IUserGridLayoutRepository, UserGridLayoutRepository>();

        // Kargo Katip modülü repository'leri
        services.AddScoped<ICompanyDirectoryRepository, CompanyDirectoryRepository>();
        services.AddScoped<ICompanyAttentionContactRepository, CompanyAttentionContactRepository>();
        services.AddScoped<ICargoCompanyRepository, CargoCompanyRepository>();
        services.AddScoped<ICargoShipmentRepository, CargoShipmentRepository>();

        // Uygulama ayarları
        services.AddScoped<IApplicationSettingRepository, ApplicationSettingRepository>();
        // AES-256: tüm makineler + tüm Windows kullanıcıları aynı anahtarı paylaşır
        services.AddSingleton<ISecretProtector, AesSecretProtector>();
        services.AddSingleton<IMailSettingsService, MailSettingsService>();

        // Dashboard cache — Singleton: tüm oturumlarda tek önbellek
        services.AddSingleton<ICargoDashboardCacheService, InMemoryCargoDashboardCacheService>();

        services.AddScoped<IAuthenticationService, DatabaseAuthenticationService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IReportExportService, ReportExportService>();
        services.AddScoped<IUserGridLayoutService, UserGridLayoutService>();
        services.AddScoped<IDatabaseConnectionTestService, DatabaseConnectionTestService>();
        services.AddScoped<IHealthCheckService, HealthCheckService>();
        services.AddSingleton<IErrorNotificationService, NullErrorNotificationService>();
        // Singleton: kendi içinde scope açarak Scoped AppDbContext'e güvenle erişir
        services.AddSingleton<ISystemLogService, SystemLogService>();
        // Singleton: startup'ta bir kez arka planda çalışır; IServiceProvider ile kendi scope'unu açar
        services.AddSingleton<ICargoRetentionService, CargoRetentionService>();
        services.AddScoped<GetDashboardHandler>();

        // [DEV-ONLY] Geliştirme ortamı seed servisi
        services.AddScoped<IDevDataSeeder, DevDataSeeder>();

        // Mail ayarları ilk çalıştırma seed'i
        services.AddScoped<MailSettingsSeeder>();

        // EF Core migration uygulayıcı — UI katmanı EF Core referansı olmadan çağırır
        services.AddScoped<DatabaseMigrator>();

        return services;
    }
}
