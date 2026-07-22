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
    /// Harf dönüşüm tercihini oturuma yazar. Login'de ve ayar kaydında çağrılır;
    /// tercih anında etkinleşir, yeniden giriş gerektirmez.
    /// </summary>
    void SetTextCasePreference(TextCasePreference preference);

    /// <summary>
    /// Logout sonrası scope dispose edildikten sonra çağrılır.
    /// Bir sonraki oturuma önceki kullanıcı bilgisi taşınmaz.
    /// </summary>
    void Clear();
}
