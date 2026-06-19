using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// IUserContext (okuma) ve IUserSession (yazma) singleton implementasyonu.
/// IUserContext: handler'lar tarafından okunur.
/// IUserSession: LoginViewModel (SetUser) ve App.xaml.cs (Clear) tarafından yazılır.
/// </summary>
public sealed class UserContext : IUserContext, IUserSession
{
    public Guid   UserId   { get; private set; }
    public string FullName { get; private set; } = string.Empty;

    public void SetUser(Guid userId, string fullName)
    {
        UserId   = userId;
        FullName = fullName;
    }

    /// <summary>
    /// Logout sonrası çağrılır. Önceki kullanıcı verisi bir sonraki oturuma taşınmaz.
    /// </summary>
    public void Clear()
    {
        UserId   = Guid.Empty;
        FullName = string.Empty;
    }
}
