namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Oturum yazma işlemleri. IUserContext okuma, IUserSession yazma sorumluluğunu taşır.
/// UI katmanı: LoginViewModel (SetUser), App.xaml.cs (Clear) kullanır.
/// </summary>
public interface IUserSession
{
    void SetUser(Guid userId, string fullName);

    /// <summary>
    /// Logout sonrası scope dispose edildikten sonra çağrılır.
    /// Bir sonraki oturuma önceki kullanıcı bilgisi taşınmaz.
    /// </summary>
    void Clear();
}
