using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Pencere bazlı metin/zemin taraması.
///
/// Kontrast testi TOKEN ÇİFTLERİNİ ölçer — "Theme.Info.Text, Theme.Info.Background
/// üzerinde okunur mu?". Bu test bir adım öteye gider: her pencerenin gerçek
/// görsel ağacını kurar, her metin öğesinin ÇÖZÜLMÜŞ rengini ve o metnin
/// gerçekten üzerinde durduğu en yakın dolgulu yüzeyi bulur, ikisini karşılaştırır.
///
/// Yakaladığı hata sınıfı: token'lar doğruyken YANLIŞ EŞLEŞTİRİLMESİ. Örneğin
/// koyu bir rozet dolgusunun üzerine Theme.Text yazmak. Faz B manuel testinde
/// bildirilen "koyu zemin üzerinde koyu metin" şikâyetlerinin kaynağı budur.
///
/// SINIRLILIK: Yalnızca mantıksal ağaçta doğrudan bulunan öğeler taranır.
/// DataTemplate/ControlTemplate içeriği veri bağlanmadan üretilmez; oralar
/// ControlStateMatrixTests ve ThemeContrastTests ile kapsanır.
/// </summary>
public class WindowTreeContrastTests
{
    /// <summary>
    /// Tam WCAG AA. Taranan 213 metin öğesinin tamamı iki temada da bu eşiği
    /// geçiyor; eşiği düşürmek sessiz gerilemeye kapı açardı.
    /// </summary>
    private const double Threshold = Contrast.AA;

    public static TheoryData<string> ThemeNames() => [ThemeTestHost.Light, ThemeTestHost.Dark];

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Pencerelerdeki_metinler_zeminleriyle_ayrisiyor(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);

        var failures  = new List<string>();
        var scanned   = 0;
        var windows   = 0;

        foreach (var file in UiSourceLocator.XamlFiles(includeResources: false))
        {
            var relative = UiSourceLocator.Relative(file);

            var root = ThemeTestHost.Run(() => XamlSanitizer.TryParse(file));
            if (root is null) continue;   // ayrıştırma hataları XamlLoadTests'in işi
            windows++;

            ThemeTestHost.Run(() =>
            {
                foreach (var text in Descendants(root).OfType<TextBlock>())
                {
                    if (text.Visibility == Visibility.Collapsed) continue;
                    if (string.IsNullOrWhiteSpace(text.Text)) continue;

                    var fg = text.Foreground as SolidColorBrush;
                    if (fg is null || fg.Color.A == 0) continue;

                    var bg = NearestOpaqueBackground(text);
                    if (bg is null) continue;

                    scanned++;

                    var ratio = Contrast.Ratio(fg.Color, bg.Value.Color);
                    if (ratio < Threshold)
                    {
                        failures.Add(
                            $"{relative}: \"{Shorten(text.Text)}\" — metin {Contrast.Describe(fg.Color)} / " +
                            $"zemin {Contrast.Describe(bg.Value.Color)} ({bg.Value.Owner}) = {ratio:F2}:1");
                    }
                }
            });
        }

        Assert.True(windows >= 35, $"Yalnızca {windows} pencere taranabildi.");
        Assert.True(scanned  >= 150, $"Yalnızca {scanned} metin öğesi ölçülebildi — tarama beklenenden dar.");

        Assert.True(failures.Count == 0,
            $"{themeName} — {failures.Count} okunmayan metin ({scanned} öğe tarandı):{Environment.NewLine}" +
            string.Join(Environment.NewLine, failures));
    }

    private readonly record struct Surface(SolidColorBrush Brush, string Owner)
    {
        public Color Color => Brush.Color;
    }

    /// <summary>
    /// Metnin gerçekten üzerinde durduğu yüzeyi bulur: mantıksal ağaçta yukarı
    /// çıkıp dolgusu olan (şeffaf olmayan) ilk kabı arar. Hiçbiri yoksa pencere
    /// zeminine düşer — Window stilinde Background artık tanımlıdır.
    /// </summary>
    private static Surface? NearestOpaqueBackground(DependencyObject start)
    {
        for (var node = LogicalTreeHelper.GetParent(start); node is not null; node = LogicalTreeHelper.GetParent(node))
        {
            var brush = node switch
            {
                Panel p      => p.Background,
                Border b     => b.Background,
                Control c    => c.Background,
                _            => null
            };

            if (brush is SolidColorBrush solid && solid.Color.A > 0)
                return new Surface(solid, node.GetType().Name);
        }

        return null;
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var nested in Descendants(child))
                yield return nested;
        }
    }

    private static string Shorten(string text)
    {
        var single = text.Replace(Environment.NewLine, " ").Replace('\n', ' ').Trim();
        return single.Length <= 40 ? single : single[..37] + "...";
    }
}
