using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

// Sprint 21: işlem başına taze DbContext (IDbContextFactory).
public class UserRepository : IUserRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public UserRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<User?> GetByIdAsync(Guid id)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.Users.FirstOrDefaultAsync(x => x.Id == id);
    }

    // Case-insensitive arama — kullanıcı adı büyük/küçük harf duyarsız kontrol edilir
    public async Task<User?> GetByUserNameAsync(string userName)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.Users
            .FirstOrDefaultAsync(x => x.UserName.ToLower() == userName.ToLower());
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.Users.ToListAsync();
    }

    public async Task AddAsync(User user)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        await ctx.Users.AddAsync(user);
        await ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.Users.Update(user);
        await ctx.SaveChangesAsync();
    }

    /// <summary>Fiziksel silme yapmaz; kaydı soft-delete olarak işaretler.</summary>
    public async Task DeleteAsync(Guid id)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var user = await ctx.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null) return;
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }
}
