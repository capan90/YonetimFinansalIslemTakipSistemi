using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

public class UserGridLayoutRepository : IUserGridLayoutRepository
{
    private readonly AppDbContext _context;

    public UserGridLayoutRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetLayoutJsonAsync(Guid userId, string screenKey)
    {
        var layout = await _context.UserGridLayouts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ScreenKey == screenKey);
        return layout?.LayoutJson;
    }

    public async Task SaveLayoutJsonAsync(Guid userId, string screenKey, string layoutJson)
    {
        var existing = await _context.UserGridLayouts
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ScreenKey == screenKey);

        if (existing is null)
        {
            _context.UserGridLayouts.Add(new UserGridLayout
            {
                Id         = Guid.NewGuid(),
                UserId     = userId,
                ScreenKey  = screenKey,
                LayoutJson = layoutJson,
                UpdatedAt  = DateTime.UtcNow
            });
        }
        else
        {
            existing.LayoutJson = layoutJson;
            existing.UpdatedAt  = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteLayoutAsync(Guid userId, string screenKey)
    {
        var existing = await _context.UserGridLayouts
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ScreenKey == screenKey);

        if (existing is null) return;

        _context.UserGridLayouts.Remove(existing);
        await _context.SaveChangesAsync();
    }
}
