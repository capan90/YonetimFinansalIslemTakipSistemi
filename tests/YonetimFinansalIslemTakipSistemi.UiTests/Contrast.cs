using System.Windows.Media;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// WCAG 2.1 bağıl parlaklık ve kontrast oranı.
/// Formül: https://www.w3.org/TR/WCAG21/#dfn-contrast-ratio
/// </summary>
public static class Contrast
{
    /// <summary>Normal boyutlu metin için WCAG AA eşiği.</summary>
    public const double AA = 4.5;

    /// <summary>
    /// Büyük metin (18pt+ veya 14pt bold) ve grafik/kenarlık için AA eşiği.
    /// Devre dışı kontroller WCAG'den muaftır; testte yine de bu alt sınır aranır
    /// ki "devre dışı" ile "görünmez" karışmasın.
    /// </summary>
    public const double AALarge = 3.0;

    public static double Ratio(Color a, Color b)
    {
        var l1 = RelativeLuminance(a);
        var l2 = RelativeLuminance(b);
        var (hi, lo) = l1 >= l2 ? (l1, l2) : (l2, l1);
        return (hi + 0.05) / (lo + 0.05);
    }

    public static double Ratio(SolidColorBrush a, SolidColorBrush b) => Ratio(a.Color, b.Color);

    private static double RelativeLuminance(Color c) =>
        0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);

    private static double Channel(byte value)
    {
        var s = value / 255.0;
        return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
    }

    public static string Describe(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
}
