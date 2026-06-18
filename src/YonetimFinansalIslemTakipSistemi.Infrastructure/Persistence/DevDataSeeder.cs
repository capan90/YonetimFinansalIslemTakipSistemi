using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

/// <summary>
/// [DEV-ONLY] IDevDataSeeder implementasyonu.
/// Users tablosu boşsa sabit bir admin kullanıcısı oluşturur.
/// Üretim ortamında bu sınıf kaldırılacak; kullanıcılar yönetim ekranından oluşturulacak.
/// </summary>
internal sealed class DevDataSeeder : IDevDataSeeder
{
    private readonly AppDbContext _context;

    // [DEV-ONLY] Geliştirme ortamı seed bilgileri — production'a taşınmadan önce kaldırılacak
    private const string DevUserName = "admin";
    private const string DevPassword = "Admin123!";
    private const string DevFullName = "Yönetici";

    public DevDataSeeder(AppDbContext context) => _context = context;

    public async Task SeedAsync()
    {
        // Herhangi bir kullanıcı varsa çalışma — idempotent; mevcut veriyi değiştirmez
        if (await _context.Users.AnyAsync())
            return;

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
    }
}
