using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

// Sprint 21: işlem başına taze DbContext (IDbContextFactory).
public class ApplicationSettingRepository : IApplicationSettingRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ApplicationSettingRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<ApplicationSetting?> GetByKeyAsync(string key)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.ApplicationSettings.FirstOrDefaultAsync(x => x.Key == key);
    }

    public async Task<IReadOnlyList<ApplicationSetting>> GetByPrefixAsync(string prefix)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.ApplicationSettings
            .Where(x => x.Key.StartsWith(prefix))
            .ToListAsync();
    }

    public async Task UpsertAsync(string key, string? value, bool isEncrypted, Guid userId)
    {
        // Okuma ve yazma aynı ctx üzerinde → tracking ile güncelleme çalışır (tek metot, tek transaction).
        await using var ctx = await _factory.CreateDbContextAsync();

        // Silinmiş kayıtlar dahil — aynı key üzerinde restore mantığı var
        var existing = await ctx.ApplicationSettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Key == key);

        if (existing is null)
        {
            var entry = new ApplicationSetting
            {
                Id              = Guid.NewGuid(),
                Key             = key,
                Value           = value,
                IsEncrypted     = isEncrypted,
                CreatedByUserId = userId,
                CreatedAt       = DateTime.UtcNow,
            };
            await ctx.ApplicationSettings.AddAsync(entry);
        }
        else
        {
            existing.Value           = value;
            existing.IsEncrypted     = isEncrypted;
            existing.UpdatedByUserId = userId;
            existing.UpdatedAt       = DateTime.UtcNow;
            existing.IsDeleted       = false; // soft-delete'ten geri al
        }

        await ctx.SaveChangesAsync();
    }
}
