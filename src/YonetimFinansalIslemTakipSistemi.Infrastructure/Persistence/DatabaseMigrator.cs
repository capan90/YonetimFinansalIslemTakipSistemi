using Microsoft.EntityFrameworkCore;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

/// <summary>
/// EF Core migration'larını uygulayan yardımcı servis.
/// UI katmanı EF Core bağımlılığına sahip olmadan çağırabilir.
/// </summary>
public class DatabaseMigrator
{
    private readonly AppDbContext _context;
    public DatabaseMigrator(AppDbContext context) => _context = context;

    public Task ApplyAsync() => _context.Database.MigrateAsync();
}
