using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

public class UserPermissionRepository : IUserPermissionRepository
{
    private readonly AppDbContext _context;

    public UserPermissionRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlySet<PermissionType>> GetByUserIdAsync(Guid userId)
    {
        var perms = await _context.UserPermissions
            .Where(p => p.UserId == userId)
            .Select(p => p.Permission)
            .ToListAsync();

        return new HashSet<PermissionType>(perms);
    }

    public async Task UpdateAsync(Guid userId, IEnumerable<PermissionType> permissions)
    {
        // Transaction: eski izinleri sil → yenilerini ekle — yarıda kalırsa kullanıcı izinsiz bırakılmaz
        await using var tx = await _context.Database.BeginTransactionAsync();

        var existing = await _context.UserPermissions
            .Where(p => p.UserId == userId)
            .ToListAsync();
        _context.UserPermissions.RemoveRange(existing);
        await _context.SaveChangesAsync();

        var newEntries = permissions.Select(p => new UserPermission
        {
            UserId     = userId,
            Permission = p
        });
        await _context.UserPermissions.AddRangeAsync(newEntries);
        await _context.SaveChangesAsync();

        await tx.CommitAsync();
    }

    public async Task<bool> AnyOtherActiveUserHasPermissionAsync(
        PermissionType permission, Guid excludeUserId)
    {
        // Başka aktif ve silinmemiş bir kullanıcının bu yetkisi var mı?
        return await _context.UserPermissions
            .Where(p => p.Permission == permission && p.UserId != excludeUserId)
            .Join(_context.Users,
                  perm => perm.UserId,
                  user => user.Id,
                  (perm, user) => user)
            .AnyAsync(u => u.IsActive && !u.IsDeleted);
    }
}
