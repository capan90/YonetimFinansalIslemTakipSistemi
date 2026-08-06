using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using Serilog;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.Services;

/// <summary>
/// UI katmanında yaşar çünkü ApplyTheme() WPF Application.Current'a erişir.
/// DB erişimi için IServiceScopeFactory ile Scoped scope açar.
/// Singleton olarak kaydedilir.
/// </summary>
public class ThemeService : IThemeService
{
    private const string ThemeKey        = "UI:Theme";
    private const string DefaultTheme    = "Light";
    private const string LightThemeName  = "Light";
    private const string DarkThemeName   = "Dark";

    /// <summary>Windows'un açık/koyu ayarını takip eden üçüncü seçenek.</summary>
    private const string SystemThemeName = "System";

    // Windows kişiselleştirme anahtarı: AppsUseLightTheme 0 = koyu, 1 = açık
    private const string PersonalizeKey   = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightName = "AppsUseLightTheme";

    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// "System" seçiliyken Windows teması değişirse uygulama da anında dönmelidir.
    /// Abonelik yalnızca System seçiliyken açık tutulur — diğer modlarda OS
    /// değişikliği kullanıcının bilinçli seçimini ezmemeli.
    /// </summary>
    private bool _followingSystem;

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

        // "System" saklanan tercihtir; uygulanacak gerçek tema OS'tan çözülür
        var effective = theme == SystemThemeName ? ResolveSystemTheme() : theme;

        UpdateSystemThemeSubscription(theme == SystemThemeName);

        var themeName = effective == DarkThemeName ? "DarkTheme" : "LightTheme";
        var newUri    = new Uri($"pack://application:,,,/Resources/Themes/{themeName}.xaml");

        // System.Windows.Application.Current — tam niteleme: Application ismi YonetimFinansalIslemTakipSistemi.Application namespace'i ile çakışır
        var dicts = System.Windows.Application.Current.Resources.MergedDictionaries;

        // Mevcut tema sözlüğünü bul ve çıkar
        var old = dicts.FirstOrDefault(d =>
            d.Source != null &&
            (d.Source.OriginalString.Contains("LightTheme") ||
             d.Source.OriginalString.Contains("DarkTheme")));

        // Aynı tema zaten yüklüyse dokunma: sözlüğü çıkarıp yeniden eklemek
        // tüm DynamicResource'ları gereksiz yere yeniden çözdürür
        if (old is not null && old.Source!.OriginalString.Contains(themeName)) return;

        if (old != null) dicts.Remove(old);

        // Yeni temayı sona ekle (son kayıt önceliklidir)
        dicts.Add(new ResourceDictionary { Source = newUri });

        // XAML'deki DynamicResource'lar buraya kadar kendiliğinden güncellendi.
        // Grafikler GÜNCELLENMEDİ: LiveCharts SkiaSharp ile çizer ve kaynak
        // sözlüğünü izlemez. Açık pencerelerin serilerini yeniden kurması için
        // ayrıca haber verilir (bkz. Common/ChartPalette.cs).
        ChartPalette.NotifyThemeChanged();
    }

    /// <summary>
    /// Windows'un uygulama teması tercihini okur. Anahtar yoksa (eski Windows
    /// sürümleri) veya okunamıyorsa açık temaya düşülür.
    /// </summary>
    private static string ResolveSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            var value = key?.GetValue(AppsUseLightName);
            if (value is int i) return i == 0 ? DarkThemeName : LightThemeName;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Windows tema tercihi okunamadı — açık temaya düşülüyor");
        }

        return LightThemeName;
    }

    private void UpdateSystemThemeSubscription(bool follow)
    {
        if (follow == _followingSystem) return;

        if (follow) SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        else        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;

        _followingSystem = follow;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General) return;

        // Olay UI thread'inde gelmeyebilir; ResourceDictionary değişimi UI thread ister
        System.Windows.Application.Current?.Dispatcher.Invoke(() => ApplyTheme(SystemThemeName));
    }

    /// <summary>
    /// Geçerli tema değerleri. Faz B'de "Dark" ve "System" açıldı —
    /// tüm ekranlar DynamicResource token'larına taşındıktan sonra.
    /// </summary>
    private static bool IsValidTheme(string? theme) =>
        theme is LightThemeName or DarkThemeName or SystemThemeName;
}
