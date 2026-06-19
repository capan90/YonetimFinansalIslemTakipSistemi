using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

/// <summary>
/// [DEV-ONLY] Geliştirme ortamı seed servisi.
/// Admin yoksa oluşturur; admin mevcutsa eksik izinleri tamamlar.
/// PermissionType enum'una yeni değer eklendiğinde upgrade-safe çalışır — mevcut izinleri silmez.
/// Üretim ortamında bu sınıf kaldırılacak.
/// </summary>
internal sealed class DevDataSeeder : IDevDataSeeder
{
    private readonly AppDbContext _context;

    private const string DevUserName = "admin";
    private const string DevPassword = "Admin123!";
    private const string DevFullName = "Yönetici";

    public DevDataSeeder(AppDbContext context) => _context = context;

    public async Task SeedAsync()
    {
        // 1. Admin yoksa oluştur
        var admin = await _context.Users.FirstOrDefaultAsync(u => u.UserName == DevUserName);
        if (admin is null)
        {
            admin = new User
            {
                Id           = Guid.NewGuid(),
                UserName     = DevUserName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DevPassword),
                FullName     = DevFullName,
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow
            };

            await _context.Users.AddAsync(admin);
            await _context.SaveChangesAsync();
        }

        // 2. Admin'in eksik izinlerini tamamla
        // Upgrade senaryosu: enum'a yeni değer (örn. CanViewReports=6) eklendiğinde
        // mevcut izinler silinmez, yalnızca eksik olanlar eklenir.
        await SeedMissingPermissionsAsync(admin.Id);
    }

    private async Task SeedMissingPermissionsAsync(Guid userId)
    {
        var existing = await _context.UserPermissions
            .Where(p => p.UserId == userId)
            .Select(p => p.Permission)
            .ToListAsync();

        var missing = Enum.GetValues<PermissionType>()
                          .Except(existing)
                          .Select(p => new UserPermission { UserId = userId, Permission = p })
                          .ToList();

        if (missing.Count == 0) return;

        await _context.UserPermissions.AddRangeAsync(missing);
        await _context.SaveChangesAsync();
    }
}
