using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

public class DatabaseAuthenticationService : IAuthenticationService
{
    private readonly IUserRepository           _userRepository;
    private readonly IUserPermissionRepository _permissionRepository;
    private readonly IAuditLogService          _auditLogService;

    public DatabaseAuthenticationService(
        IUserRepository           userRepository,
        IUserPermissionRepository permissionRepository,
        IAuditLogService          auditLogService)
    {
        _userRepository       = userRepository;
        _permissionRepository = permissionRepository;
        _auditLogService      = auditLogService;
    }

    public async Task<AuthResult> AuthenticateAsync(string userName, string password)
    {
        var user = await _userRepository.GetByUserNameAsync(userName);

        // Kullanıcı bulunamadı veya soft-delete ile silinmiş
        if (user is null)
            return AuthResult.Fail("Kullanıcı adı veya şifre hatalı.");

        // Devre dışı hesap — güvenlik gereği asıl nedenin açıklanmaması
        if (!user.IsActive)
            return AuthResult.Fail("Bu hesap devre dışı bırakılmış.");

        // BCrypt hash doğrulaması — plaintext şifre hiçbir zaman DB'ye yazılmaz
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return AuthResult.Fail("Kullanıcı adı veya şifre hatalı.");

        // Giriş sonrası kullanıcı izinleri yüklenir; oturum boyunca IUserContext üzerinden okunur
        var permissions = await _permissionRepository.GetByUserIdAsync(user.Id);

        // Audit: başarılı giriş
        await _auditLogService.WriteAsync(
            AuditAction.UserLoggedIn,
            user.Id,
            user.FullName,
            "User", user.Id);

        return AuthResult.Ok(user.Id, user.FullName, permissions);
    }
}
