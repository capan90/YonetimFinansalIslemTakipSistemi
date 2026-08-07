using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Navigasyon rayının okunabilirliği (Faz D7.1).
///
/// YAKALADIĞI HATA SINIFI: token doğru, kontrol onu OKUMUYOR.
///
/// Ray her iki temada da KOYU bir yüzey (Theme.Nav.Background). Ray listesi
/// örtük ListBoxItem stilini kullanıyordu; o stil gövde metni için tasarlandı
/// ve Theme.Text okur. Açık temada Theme.Text = #1A202C, yani koyu lacivert
/// üstünde neredeyse siyah → 1,40:1. Koyu temada Theme.Text açık renk olduğu
/// için hata GÖRÜNMÜYORDU; token taramaları da temizdi çünkü ham renk yoktu.
///
/// Bu yüzden test "hangi token tanımlı" diye değil, "kontrol hangi rengi
/// çözüyor" diye soruyor.
/// </summary>
public class NavigationRailThemeTests
{
    public static TheoryData<string> ThemeNames() => [ThemeTestHost.Light, ThemeTestHost.Dark];

    /// <summary>
    /// Örtük stiller yalnızca ağaca bağlı ve ölçülmüş öğelere uygulanır;
    /// kopuk bir kontrolde Foreground null kalır.
    /// </summary>
    private static T Materialize<T>(T element) where T : FrameworkElement
    {
        var host = new Border { Child = element };
        host.Measure(new Size(400, 400));
        host.Arrange(new Rect(0, 0, 400, 400));
        return element;
    }

    private static Color Resolve(string key)
    {
        var brush = (SolidColorBrush)WpfApp.Current.FindResource(key);
        return brush.Color;
    }

    /// <summary>Ray stilinin ham işaretlemesi — token kaynağı buradan okunur.</summary>
    private static string NavStyle(string styleKey)
    {
        var controls = File.ReadAllText(
            Path.Combine(UiSourceLocator.UiProjectDirectory, "Resources", "Controls.xaml"), Encoding.UTF8);

        var style = Regex.Match(controls, $@"<Style x:Key=""{styleKey}""[\s\S]*?</Style>").Value;

        Assert.NotEmpty(style);
        return style;
    }

    /// <summary>
    /// Rayda seçili OLMAYAN öğe, rayın zemininde okunabilmeli. Kullanıcının
    /// bildirdiği kusur tam olarak buydu.
    /// </summary>
    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Secili_olmayan_ray_ogesi_okunabiliyor(string themeName) => ThemeTestHost.Run(() =>
    {
        ThemeTestHost.ApplyTheme(themeName);

        var item = Materialize(new ListBoxItem
        {
            Content = "Nakit İşlemler",
            Style   = (Style)WpfApp.Current.FindResource("NavListItem"),
        });

        var foreground = (SolidColorBrush)item.Foreground;
        var background = Resolve("Theme.Nav.Background");

        var ratio = Contrast.Ratio(foreground.Color, background);

        Assert.True(ratio >= Contrast.AA,
            $"{themeName} — ray öğesi {Contrast.Describe(foreground.Color)} / " +
            $"zemin {Contrast.Describe(background)} = {ratio:F2}:1");
    });

    /// <summary>
    /// Ray öğesi ray token'ına çözülmeli — her iki temada.
    /// </summary>
    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Ray_ogesi_nav_tokenina_cozuluyor(string themeName) => ThemeTestHost.Run(() =>
    {
        ThemeTestHost.ApplyTheme(themeName);

        var item = Materialize(new ListBoxItem
        {
            Content = "Nakit İşlemler",
            Style   = (Style)WpfApp.Current.FindResource("NavListItem"),
        });

        Assert.Equal(Resolve("Theme.Nav.Foreground"), ((SolidColorBrush)item.Foreground).Color);
    });

    /// <summary>
    /// Ray öğesi GÖVDE METNİ token'ını kullanmamalı. Theme.Text açık temada
    /// koyu, koyu temada açıktır; koyu bir yüzeyde yalnızca birinde çalışır.
    /// Kusurun kök nedeni buydu.
    ///
    /// Bu iddia ÇALIŞMA ZAMANI DEĞERİYLE kurulamaz: koyu temada Theme.Text ve
    /// Theme.Nav.Foreground aynı renge (#F1F5F9) çözülür, yani "yanlış token
    /// okunuyor" ile "doğru token okunuyor" ayırt edilemez. Soru zaten renk
    /// değil KAYNAK sorusu olduğu için stilin kendisine bakılıyor.
    /// </summary>
    [Fact]
    public void Ray_ogesi_govde_metni_tokenini_kullanmiyor()
    {
        var style = NavStyle("NavListItem");

        Assert.Contains("Theme.Nav.Foreground", style, StringComparison.Ordinal);
        Assert.DoesNotContain("Theme.Text", style, StringComparison.Ordinal);
    }

    /// <summary>
    /// Kabuk rayı bu stili gerçekten kullanmalı — stil tanımlı olup
    /// bağlanmazsa hata aynen sürerdi.
    /// </summary>
    [Fact]
    public void Kabuk_rayi_nav_stilini_kullaniyor()
    {
        var markup = Regex.Replace(
            File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
                "Views", "Shell", "ShellWindow.xaml"), Encoding.UTF8),
            @"<!--.*?-->", string.Empty, RegexOptions.Singleline);

        var navList = Regex.Match(markup, @"<ListBox\b[\s\S]*?/>").Value;

        Assert.NotEmpty(navList);
        Assert.Contains(@"Style=""{StaticResource NavList}""", navList, StringComparison.Ordinal);
        Assert.Contains(@"ItemContainerStyle=""{StaticResource NavListItem}""", navList, StringComparison.Ordinal);
    }

    /// <summary>
    /// Düzeltme TOKEN SEVİYESİNDE kalmalı: ray stilinde tek tek renk
    /// verilmemeli, mevcut Nav token ailesi okunmalı.
    /// </summary>
    [Fact]
    public void Nav_stilleri_ham_renk_icermiyor()
    {
        foreach (var styleKey in new[] { "NavList", "NavListItem" })
        {
            var style = NavStyle(styleKey);

            var rawColors = Regex.Matches(style, @"Value=""#[0-9A-Fa-f]{3,8}""")
                                 .Select(m => m.Value)
                                 .ToList();

            Assert.True(rawColors.Count == 0,
                $"{styleKey} ham renk içeriyor: " + string.Join(", ", rawColors));
        }
    }

    /// <summary>
    /// Grup başlıkları da aynı koyu zeminde durur; onlar zaten
    /// Theme.Nav.Foreground kullanıyordu ve öyle kalmalı.
    /// </summary>
    [Fact]
    public void Grup_basliklari_nav_tokenini_kullaniyor()
    {
        var markup = File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
            "Views", "Shell", "ShellWindow.xaml"), Encoding.UTF8);

        var header = Regex.Match(markup, @"<TextBlock Text=""\{Binding Title\}""[\s\S]*?/>").Value;

        Assert.NotEmpty(header);
        Assert.Contains("Theme.Nav.Foreground", header, StringComparison.Ordinal);
    }
}
