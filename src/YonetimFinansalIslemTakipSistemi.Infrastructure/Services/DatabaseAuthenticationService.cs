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
    private readonly ISystemLogService         _systemLog;

    public DatabaseAuthenticationService(
        IUserRepository           userRepository,
        IUserPermissionRepository permissionRepository,
        IAuditLogService          auditLogService,
        ISystemLogService         systemLog)
    {
        _userRepository       = userRepository;
        _permissionRepository = permissionRepository;
        _auditLogService      = auditLogService;
        _systemLog            = systemLog;
    }

    public async Task<AuthResult> AuthenticateAsync(string userName, string password)
    {
        var user = await _userRepository.GetByUserNameAsync(userName);

        // Başarısız denemeler System Log'a yazılır (brute-force izlenebilirliği);
        // şifre hiçbir koşulda loglanmaz, kullanıcıya asıl neden açıklanmaz.

        // Kullanıcı bulunamadı veya soft-delete ile silinmiş
        if (user is null)
        {
            await _systemLog.LogWarningAsync("Auth",
                $"Başarısız giriş denemesi: bilinmeyen kullanıcı adı '{userName}'.",
                source: nameof(DatabaseAuthenticationService));
            return AuthResult.Fail("Kullanıcı adı veya şifre hatalı.");
        }

        // Devre dışı hesap — güvenlik gereği asıl nedenin açıklanmaması
        if (!user.IsActive)
        {
            await _systemLog.LogWarningAsync("Auth",
                $"Başarısız giriş denemesi: devre dışı hesap '{userName}'.",
                source: nameof(DatabaseAuthenticationService));
            return AuthResult.Fail("Bu hesap devre dışı bırakılmış.");
        }

        // BCrypt hash doğrulaması — plaintext şifre hiçbir zaman DB'ye yazılmaz
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            await _systemLog.LogWarningAsync("Auth",
                $"Başarısız giriş denemesi: hatalı şifre, kullanıcı '{userName}'.",
                source: nameof(DatabaseAuthenticationService));
            return AuthResult.Fail("Kullanıcı adı veya şifre hatalı.");
        }

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
