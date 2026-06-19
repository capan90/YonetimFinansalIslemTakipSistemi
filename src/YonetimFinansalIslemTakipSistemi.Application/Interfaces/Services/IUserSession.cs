using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Oturum yazma işlemleri. IUserContext okuma, IUserSession yazma sorumluluğunu taşır.
/// LoginViewModel (SetUser) ve App.xaml.cs (Clear) kullanır.
/// </summary>
public interface IUserSession
{
    /// <summary>
    /// Başarılı girişte kimlik ve yetkilerle birlikte oturumu başlatır.
    /// </summary>
    void SetUser(Guid userId, string fullName, IReadOnlySet<PermissionType> permissions);

    /// <summary>
    /// Logout sonrası scope dispose edildikten sonra çağrılır.
    /// Bir sonraki oturuma önceki kullanıcı bilgisi taşınmaz.
    /// </summary>
    void Clear();
}
