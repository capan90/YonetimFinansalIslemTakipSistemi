using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<CashTransaction>    CashTransactions    => Set<CashTransaction>();
    public DbSet<User>               Users               => Set<User>();
    public DbSet<AuditLog>           AuditLogs           => Set<AuditLog>();
    public DbSet<UserPermission>     UserPermissions     => Set<UserPermission>();
    public DbSet<ExchangeRate>       ExchangeRates       => Set<ExchangeRate>();
    public DbSet<UserGridLayout>     UserGridLayouts     => Set<UserGridLayout>();

    // Kargo Katip modülü
    public DbSet<CompanyDirectory>          CompanyDirectories         => Set<CompanyDirectory>();
    public DbSet<CompanyAttentionContact>   CompanyAttentionContacts   => Set<CompanyAttentionContact>();
    public DbSet<CargoCompany>              CargoCompanies             => Set<CargoCompany>();
    public DbSet<CargoShipment>             CargoShipments             => Set<CargoShipment>();
    public DbSet<CargoNumberCounter>        CargoNumberCounters        => Set<CargoNumberCounter>();

    // Ortak WhatsApp rehberi
    public DbSet<WhatsAppContact>           WhatsAppContacts           => Set<WhatsAppContact>();

    // Ortak mail rehberi — kargo bildirim maillerinde alıcı/CC seçimi
    public DbSet<MailContact>               MailContacts               => Set<MailContact>();

    // Kullanıcı tercihleri (harf duyarlılığı vb.)
    public DbSet<UserPreference>            UserPreferences            => Set<UserPreference>();

    // Genel uygulama ayarları
    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();

    // Sistem Logları modülü
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tüm IEntityTypeConfiguration implementasyonları bu assembly'den otomatik yüklenir
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
