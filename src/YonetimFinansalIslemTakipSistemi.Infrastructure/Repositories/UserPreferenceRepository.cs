using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

public class UserPreferenceRepository : IUserPreferenceRepository
{
    private readonly AppDbContext _context;

    public UserPreferenceRepository(AppDbContext context) => _context = context;

    public async Task<UserPreference?> GetByUserIdAsync(Guid userId)
        => await _context.UserPreferences.FirstOrDefaultAsync(x => x.UserId == userId);

    public async Task AddAsync(UserPreference entity)
    {
        await _context.UserPreferences.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserPreference entity)
    {
        _context.UserPreferences.Update(entity);
        await _context.SaveChangesAsync();
    }
}
