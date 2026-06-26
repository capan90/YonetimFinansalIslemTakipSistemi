using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

public class ApplicationSettingRepository : IApplicationSettingRepository
{
    private readonly AppDbContext _context;

    public ApplicationSettingRepository(AppDbContext context) => _context = context;

    public async Task<ApplicationSetting?> GetByKeyAsync(string key)
        => await _context.ApplicationSettings.FirstOrDefaultAsync(x => x.Key == key);

    public async Task<IReadOnlyList<ApplicationSetting>> GetByPrefixAsync(string prefix)
        => await _context.ApplicationSettings
            .Where(x => x.Key.StartsWith(prefix))
            .ToListAsync();

    public async Task UpsertAsync(string key, string? value, bool isEncrypted, Guid userId)
    {
        // Silinmiş kayıtlar dahil — aynı key üzerinde restore mantığı var
        var existing = await _context.ApplicationSettings
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
            await _context.ApplicationSettings.AddAsync(entry);
        }
        else
        {
            existing.Value           = value;
            existing.IsEncrypted     = isEncrypted;
            existing.UpdatedByUserId = userId;
            existing.UpdatedAt       = DateTime.UtcNow;
            existing.IsDeleted       = false; // soft-delete'ten geri al
        }

        await _context.SaveChangesAsync();
    }
}
