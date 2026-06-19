using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

/// <summary>
/// [DEV-ONLY] Geliştirme ortamı seed servisi.
/// Boş veritabanında admin kullanıcısını ve tüm izinlerini oluşturur.
/// Admin mevcutsa fakat izinleri eksikse izinleri tamamlar (migration sonrası upgrade senaryosu).
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
        if (!await _context.Users.AnyAsync())
        {
            // Hiç kullanıcı yok → admin + tüm izinleri oluştur
            var admin = new User
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

            await SeedPermissionsAsync(admin.Id);
        }
        else
        {
            // Admin mevcutsa izinlerini kontrol et — permissions tablosu sonradan eklenmiş olabilir
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == DevUserName);
            if (adminUser is not null && !await _context.UserPermissions.AnyAsync(p => p.UserId == adminUser.Id))
                await SeedPermissionsAsync(adminUser.Id);
        }
    }

    private async Task SeedPermissionsAsync(Guid userId)
    {
        // Admin tüm izinlerle başlar
        var perms = Enum.GetValues<PermissionType>()
                        .Select(p => new UserPermission { UserId = userId, Permission = p });

        await _context.UserPermissions.AddRangeAsync(perms);
        await _context.SaveChangesAsync();
    }
}
