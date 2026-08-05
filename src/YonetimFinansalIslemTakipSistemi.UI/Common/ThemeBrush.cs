using System.Windows;
using System.Windows.Media;

namespace YonetimFinansalIslemTakipSistemi.UI.Common;

/// <summary>
/// Kod tarafından tema fırçası çözer.
///
/// XAML'de <c>{DynamicResource Theme.X}</c> kullanılır; converter ve code-behind
/// bunu yapamaz ve sabit <c>Brushes.Black</c> gibi değerler koyu temada okunmaz
/// hale gelir. Bu yardımcı aynı token'ı aktif tema sözlüğünden okur.
///
/// SINIRLILIK: Converter'lar bağlama başına bir kez çalışır. Tema değişiminde
/// zaten çizilmiş hücreler eski fırçayı korur; liste yenilendiğinde düzelir.
/// XAML tarafındaki DynamicResource'lar anında güncellenir.
/// </summary>
public static class ThemeBrush
{
    /// <summary>
    /// Token'ı aktif tema sözlüğünden okur. Bulunamazsa <paramref name="fallback"/>
    /// döner — tasarım zamanı (Application.Current null) ve olası yazım hatası için.
    /// </summary>
    public static Brush Get(string key, Brush? fallback = null)
    {
        var resource = System.Windows.Application.Current?.TryFindResource(key);
        return resource as Brush ?? fallback ?? Brushes.Gray;
    }
}
