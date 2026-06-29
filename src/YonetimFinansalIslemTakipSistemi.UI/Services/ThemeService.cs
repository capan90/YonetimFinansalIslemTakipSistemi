using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.UI.Services;

/// <summary>
/// UI katmanında yaşar çünkü ApplyTheme() WPF Application.Current'a erişir.
/// DB erişimi için IServiceScopeFactory ile Scoped scope açar.
/// Singleton olarak kaydedilir.
/// </summary>
public class ThemeService : IThemeService
{
    private const string ThemeKey      = "UI:Theme";
    private const string DefaultTheme  = "Light";
    private const string DarkThemeName = "Dark";

    private readonly IServiceScopeFactory _scopeFactory;

    public ThemeService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<string> GetCurrentThemeAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IApplicationSettingRepository>();
            var setting = await repo.GetByKeyAsync(ThemeKey);
            return IsValidTheme(setting?.Value) ? setting!.Value! : DefaultTheme;
        }
        catch
        {
            return DefaultTheme;
        }
    }

    public async Task SetThemeAsync(string theme)
    {
        if (!IsValidTheme(theme)) theme = DefaultTheme;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IApplicationSettingRepository>();
            await repo.UpsertAsync(ThemeKey, theme, isEncrypted: false, userId: Guid.Empty);
        }
        catch { /* DB hatası UI'ı engellemez */ }

        ApplyTheme(theme);
    }

    public void ApplyTheme(string theme)
    {
        if (!IsValidTheme(theme)) theme = DefaultTheme;

        var themeName = theme == DarkThemeName ? "DarkTheme" : "LightTheme";
        var newUri    = new Uri($"pack://application:,,,/Resources/Themes/{themeName}.xaml");

        // System.Windows.Application.Current — tam niteleme: Application ismi YonetimFinansalIslemTakipSistemi.Application namespace'i ile çakışır
        var dicts = System.Windows.Application.Current.Resources.MergedDictionaries;

        // Mevcut tema sözlüğünü bul ve çıkar
        var old = dicts.FirstOrDefault(d =>
            d.Source != null &&
            (d.Source.OriginalString.Contains("LightTheme") ||
             d.Source.OriginalString.Contains("DarkTheme")));

        if (old != null) dicts.Remove(old);

        // Yeni temayı sona ekle (son kayıt önceliklidir)
        dicts.Add(new ResourceDictionary { Source = newUri });
    }

    // Koyu tema henüz tüm ekranlar için hazır değil; yalnızca "Light" kabul edilir.
    // DB'de "Dark" kayıtlıysa otomatik olarak "Light"a normalize edilir.
    private static bool IsValidTheme(string? theme) => theme == "Light";
}
