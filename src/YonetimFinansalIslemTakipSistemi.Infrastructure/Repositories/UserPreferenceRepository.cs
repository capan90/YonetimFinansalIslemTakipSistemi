using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

// Sprint 21: işlem başına taze DbContext (IDbContextFactory).
public class UserPreferenceRepository : IUserPreferenceRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public UserPreferenceRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<UserPreference?> GetByUserIdAsync(Guid userId)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.UserPreferences.FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task AddAsync(UserPreference entity)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        await ctx.UserPreferences.AddAsync(entity);
        await ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserPreference entity)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.UserPreferences.Update(entity);
        await ctx.SaveChangesAsync();
    }
}
