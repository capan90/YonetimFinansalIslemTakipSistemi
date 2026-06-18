using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

public class DatabaseAuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;

    public DatabaseAuthenticationService(IUserRepository userRepository)
        => _userRepository = userRepository;

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

        return AuthResult.Ok(user.Id, user.FullName);
    }
}
