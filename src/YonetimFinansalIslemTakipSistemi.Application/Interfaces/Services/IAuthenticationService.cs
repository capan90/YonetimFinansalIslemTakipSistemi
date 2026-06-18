using YonetimFinansalIslemTakipSistemi.Application.Common;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

public interface IAuthenticationService
{
    Task<AuthResult> AuthenticateAsync(string userName, string password);
}
