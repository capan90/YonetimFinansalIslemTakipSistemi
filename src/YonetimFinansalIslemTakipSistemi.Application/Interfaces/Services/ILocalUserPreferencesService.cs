namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

public interface ILocalUserPreferencesService
{
    /// <summary>%AppData%\...\user-preferences.json'dan son başarılı giriş adını okur.</summary>
    Task<string?> GetLastUsernameAsync();

    /// <summary>Başarılı girişin ardından kullanıcı adını lokal olarak kaydeder. Şifre kaydedilmez.</summary>
    Task SaveLastUsernameAsync(string username);
}
