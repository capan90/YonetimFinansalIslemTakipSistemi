using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

// Sprint 21: işlem başına taze DbContext (IDbContextFactory).
public class UserPermissionRepository : IUserPermissionRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public UserPermissionRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<IReadOnlySet<PermissionType>> GetByUserIdAsync(Guid userId)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var perms = await ctx.UserPermissions
            .Where(p => p.UserId == userId)
            .Select(p => p.Permission)
            .ToListAsync();

        return new HashSet<PermissionType>(perms);
    }

    public async Task UpdateAsync(Guid userId, IEnumerable<PermissionType> permissions)
    {
        // Transaction: eski izinleri sil → yenilerini ekle — yarıda kalırsa kullanıcı izinsiz bırakılmaz
        await using var ctx = await _factory.CreateDbContextAsync();
        await using var tx = await ctx.Database.BeginTransactionAsync();

        var existing = await ctx.UserPermissions
            .Where(p => p.UserId == userId)
            .ToListAsync();
        ctx.UserPermissions.RemoveRange(existing);
        await ctx.SaveChangesAsync();

        var newEntries = permissions.Select(p => new UserPermission
        {
            UserId     = userId,
            Permission = p
        });
        await ctx.UserPermissions.AddRangeAsync(newEntries);
        await ctx.SaveChangesAsync();

        await tx.CommitAsync();
    }

    public async Task<bool> AnyOtherActiveUserHasPermissionAsync(
        PermissionType permission, Guid excludeUserId)
    {
        // Başka aktif ve silinmemiş bir kullanıcının bu yetkisi var mı?
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.UserPermissions
            .Where(p => p.Permission == permission && p.UserId != excludeUserId)
            .Join(ctx.Users,
                  perm => perm.UserId,
                  user => user.Id,
                  (perm, user) => user)
            .AnyAsync(u => u.IsActive && !u.IsDeleted);
    }
}
