using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<CashTransaction> CashTransactions  => Set<CashTransaction>();
    public DbSet<User>            Users              => Set<User>();
    public DbSet<AuditLog>        AuditLogs          => Set<AuditLog>();
    public DbSet<UserPermission>  UserPermissions    => Set<UserPermission>();
    public DbSet<ExchangeRate>    ExchangeRates      => Set<ExchangeRate>();
    public DbSet<UserGridLayout>  UserGridLayouts    => Set<UserGridLayout>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tüm IEntityTypeConfiguration implementasyonları bu assembly'den otomatik yüklenir
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
