using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

// Sprint 21: işlem başına taze DbContext (IDbContextFactory).
public class UserGridLayoutRepository : IUserGridLayoutRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public UserGridLayoutRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<string?> GetLayoutJsonAsync(Guid userId, string screenKey)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var layout = await ctx.UserGridLayouts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ScreenKey == screenKey);
        return layout?.LayoutJson;
    }

    public async Task SaveLayoutJsonAsync(Guid userId, string screenKey, string layoutJson)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var existing = await ctx.UserGridLayouts
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ScreenKey == screenKey);

        if (existing is null)
        {
            ctx.UserGridLayouts.Add(new UserGridLayout
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

        await ctx.SaveChangesAsync();
    }

    public async Task DeleteLayoutAsync(Guid userId, string screenKey)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var existing = await ctx.UserGridLayouts
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ScreenKey == screenKey);

        if (existing is null) return;

        ctx.UserGridLayouts.Remove(existing);
        await ctx.SaveChangesAsync();
    }
}
