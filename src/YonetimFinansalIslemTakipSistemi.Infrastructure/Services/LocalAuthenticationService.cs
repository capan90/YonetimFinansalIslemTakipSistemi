using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// [DEV-ONLY / V1] Geçici sabit kullanıcı kimlik doğrulaması.
/// Üretim ortamında bu sınıf kaldırılacak; IUserRepository üzerinden
/// şifre hash doğrulaması yapan DatabaseAuthenticationService ile değiştirilecek.
/// </summary>
public class LocalAuthenticationService : IAuthenticationService
{
    private const string AdminUserName = "admin";
    private const string AdminPassword = "1234";

    public Task<AuthResult> AuthenticateAsync(string userName, string password)
    {
        if (userName == AdminUserName && password == AdminPassword)
            return Task.FromResult(AuthResult.Ok(Guid.Empty, "Yönetici", new HashSet<PermissionType>()));

        return Task.FromResult(AuthResult.Fail("Kullanıcı adı veya şifre hatalı."));
    }
}
