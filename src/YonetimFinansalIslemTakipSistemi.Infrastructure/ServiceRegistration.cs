using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        this IServiceCollection services, string connectionString,
        LogLevel efCommandLogLevel = LogLevel.Debug)
    {
        // QuestPDF Community lisansı — bu uygulama iç kullanım aracıdır, ticari ürün değildir.
        // Şirket geliri Community lisans eşiğini ($1M USD) aşarsa Professional/Enterprise lisansa geçilmeli.
        QuestPDF.Settings.License = LicenseType.Community;

        // EF Core her SQL komutunu varsayılanda Information seviyesinde yazar → tek gün 100+ MB log.
        // ConfigureWarnings ile "CommandExecuted" olayının SEVİYESİ düşürülür (varsayılan Debug):
        // global min seviye Information olduğundan komut logları yazılmaz. SQL görmek gerektiğinde
        // appsettings "Logging:EfCommandLevel" = "Information" yapılır. Bu, Serilog SourceContext
        // filtresine bağlı olmayan, EF-native ve garantili yöntemdir.
        void Configure(DbContextOptionsBuilder options) =>
            options.UseNpgsql(connectionString)
                   .ConfigureWarnings(w => w.Log((RelationalEventId.CommandExecuted, efCommandLogLevel)));

        // Sprint 21: Oturum boyunca paylaşılan tek DbContext, iki async işlem çakıştığında
        // "A second operation was started on this context instance" hatası veriyordu.
        // Çözüm: repository'ler işlem başına taze context üretir (IDbContextFactory).
        // Factory singleton'dır (durumsuz); ürettiği context'ler kısa ömürlüdür ve izole çalışır.
        services.AddDbContextFactory<AppDbContext>(Configure);

        // Startup/seed/health/systemlog/retention gibi AppDbContext'i doğrudan çözen tüketiciler
        // değişmeden çalışsın diye: scoped AppDbContext aynı factory'den türetilir (kanonik desen).
        // Bu tüketiciler ya tek seferlik (migration/seed) ya da kendi scope'unu açar → çakışma yok.
        services.AddScoped<AppDbContext>(sp =>
            sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

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

        // Ortak WhatsApp rehberi
        services.AddScoped<IWhatsAppContactRepository, WhatsAppContactRepository>();

        // Kullanıcı tercihleri (harf duyarlılığı)
        services.AddScoped<IUserPreferenceRepository, UserPreferenceRepository>();

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
        // İçe aktarma: okuyucu format bağımsız sözleşmenin Excel implementasyonudur
        services.AddScoped<ICargoImportFileReader, ExcelCargoImportReader>();
        services.AddScoped<ICargoImportTemplateService, ExcelCargoImportTemplateService>();
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
