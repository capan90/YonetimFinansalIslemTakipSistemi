namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

public interface IThemeService
{
    /// <summary>DB'den kaydedilmiş temayı döner. Kayıt yoksa "Light".</summary>
    Task<string> GetCurrentThemeAsync();

    /// <summary>Temayı DB'ye kaydeder ve anında uygular.</summary>
    Task SetThemeAsync(string theme);

    /// <summary>
    /// WPF ResourceDictionary'yi verilen tema ile değiştirir.
    /// UI thread'inden çağrılmalıdır.
    /// </summary>
    void ApplyTheme(string theme);
}
