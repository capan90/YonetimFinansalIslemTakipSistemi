using System.Windows;
using System.Windows.Media;

namespace YonetimFinansalIslemTakipSistemi.UI.Common;

/// <summary>
/// Kod tarafından tema fırçası uygular.
///
/// XAML'de <c>{DynamicResource Theme.X}</c> kullanılır. Code-behind'da bunun
/// karşılığı <see cref="FrameworkElement.SetResourceReference"/>'tır —
/// <see cref="Get"/> gibi fırçayı okuyup atamak DEĞİL.
///
/// Fark önemli: <c>element.Foreground = ThemeBrush.Get("Theme.Text")</c> o anki
/// fırça ÖRNEĞİNİ kopyalar. Tema değiştiğinde sözlükteki kayıt yenilenir ama
/// elemandaki eski örnek yerinde kalır ve kontrol eski temanın renginde donar.
/// <see cref="Apply"/> ise dinamik bir referans kurar; sözlük değişince
/// eleman kendiliğinden güncellenir.
/// </summary>
public static class ThemeBrush
{
    /// <summary>
    /// Token'ı elemana DİNAMİK olarak bağlar — tema değişiminde otomatik güncellenir.
    /// Code-behind'da renk atamanın tercih edilen yolu budur.
    /// </summary>
    /// <param name="element">Hedef eleman.</param>
    /// <param name="property">Ayarlanacak özellik (ör. <c>TextBlock.ForegroundProperty</c>).</param>
    /// <param name="key">Tema sözlüğündeki anahtar (ör. "Theme.Success").</param>
    public static void Apply(FrameworkElement element, DependencyProperty property, string key)
        => element.SetResourceReference(property, key);

    /// <summary>
    /// Token'ı aktif tema sözlüğünden OKUR. Tema değişimini takip etmez —
    /// yalnızca tek seferlik/geçici kullanımlar (ör. iki temada da aynı olan
    /// baskı renkleri) veya <see cref="FrameworkElement"/> olmayan hedefler için.
    /// Dinamik davranış gerekiyorsa <see cref="Apply"/> kullanın.
    /// Bulunamazsa <paramref name="fallback"/> döner — tasarım zamanı
    /// (Application.Current null) ve olası yazım hatası için.
    /// </summary>
    public static Brush Get(string key, Brush? fallback = null)
    {
        var resource = System.Windows.Application.Current?.TryFindResource(key);
        return resource as Brush ?? fallback ?? Brushes.Gray;
    }
}
