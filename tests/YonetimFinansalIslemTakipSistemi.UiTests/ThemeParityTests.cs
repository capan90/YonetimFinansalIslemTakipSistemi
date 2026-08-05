using System.Windows;
using System.Windows.Media;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Açık ve koyu tema sözlüklerinin yapısal denkliği.
///
/// Faz B'nin kırılma biçimi tam olarak buydu: bir rol yalnızca tek temada
/// tanımlıysa, diğer temada DynamicResource çözülemiyor ve kontrol WPF'in
/// yerleşik (tema körü) rengine düşüyor. Bu testler o boşluğu derleme
/// zamanında değil ama test zamanında yakalar.
/// </summary>
public class ThemeParityTests
{
    private static (ResourceDictionary light, ResourceDictionary dark) Themes() =>
        (ThemeTestHost.LoadTheme(ThemeTestHost.Light), ThemeTestHost.LoadTheme(ThemeTestHost.Dark));

    private static List<string> StringKeys(ResourceDictionary d) =>
        d.Keys.OfType<string>().OrderBy(k => k, StringComparer.Ordinal).ToList();

    [Fact]
    public void Light_ve_dark_ayni_anahtar_setini_tanimlar()
    {
        var (light, dark) = Themes();

        var lightKeys = StringKeys(light);
        var darkKeys  = StringKeys(dark);

        var onlyInLight = lightKeys.Except(darkKeys).ToList();
        var onlyInDark  = darkKeys.Except(lightKeys).ToList();

        Assert.True(onlyInLight.Count == 0,
            "Yalnızca LightTheme'de tanımlı: " + string.Join(", ", onlyInLight));
        Assert.True(onlyInDark.Count == 0,
            "Yalnızca DarkTheme'de tanımlı: " + string.Join(", ", onlyInDark));
    }

    [Fact]
    public void Sistem_rengi_override_lari_iki_temada_da_ayni()
    {
        var (light, dark) = Themes();

        // SystemColors anahtarları string değil ResourceKey'dir; ayrı karşılaştırılır.
        var lightSystem = light.Keys.Cast<object>().Where(k => k is not string).Select(k => k.ToString()!).OrderBy(k => k).ToList();
        var darkSystem  = dark.Keys.Cast<object>().Where(k => k is not string).Select(k => k.ToString()!).OrderBy(k => k).ToList();

        Assert.Equal(lightSystem, darkSystem);
        Assert.NotEmpty(lightSystem);
    }

    [Fact]
    public void Tum_token_lar_SolidColorBrush_veya_Color_dur()
    {
        var (light, dark) = Themes();

        foreach (var (name, dict) in new[] { (ThemeTestHost.Light, light), (ThemeTestHost.Dark, dark) })
        {
            foreach (var key in StringKeys(dict))
            {
                var value = dict[key];
                Assert.True(value is SolidColorBrush or Color,
                    $"{name}/{key} beklenmeyen tipte: {value?.GetType().Name ?? "null"}");
            }
        }
    }

    /// <summary>
    /// Faz B düzeltmesinde eklenen semantik roller. Adlar burada sabitlenir ki
    /// biri sessizce silinirse test kırılsın.
    /// </summary>
    public static TheoryData<string> RequiredRoles() =>
    [
        "Theme.Menu.Background", "Theme.Menu.Foreground",
        "Theme.Menu.HoverBackground", "Theme.Menu.HoverForeground",
        "Theme.Menu.SelectedBackground", "Theme.Menu.SelectedForeground",
        "Theme.Menu.DisabledForeground", "Theme.Menu.Separator",

        "Theme.Popup.Background", "Theme.Popup.Foreground", "Theme.Popup.Border",

        "Theme.InputBackground", "Theme.InputBorder", "Theme.InputBorderFocus",
        "Theme.Input.Placeholder",
        "Theme.Input.DisabledBackground", "Theme.Input.DisabledForeground",
        "Theme.Input.ReadOnlyBackground",
        "Theme.Input.SelectionBackground", "Theme.Input.SelectionForeground",

        "Theme.ComboBox.DropdownBackground", "Theme.ComboBox.DropdownForeground",
        "Theme.ComboBox.ItemHoverBackground", "Theme.ComboBox.ItemHoverForeground",
        "Theme.ComboBox.ItemSelectedBackground", "Theme.ComboBox.ItemSelectedForeground",

        "Theme.Nav.Background", "Theme.Nav.Foreground",
        "Theme.Nav.ItemBackground", "Theme.Nav.ItemBorder",
        "Theme.Nav.HoverBackground", "Theme.Nav.HoverForeground",
        "Theme.Nav.ActiveBackground", "Theme.Nav.ActiveForeground",

        "Theme.Info.Background", "Theme.Info.BackgroundStrong", "Theme.Info.Border", "Theme.Info.Text",
        "Theme.Warning.Background", "Theme.Warning.BackgroundStrong", "Theme.Warning.Border", "Theme.Warning.Text",
        "Theme.Danger.Background", "Theme.Danger.BackgroundStrong", "Theme.Danger.Border", "Theme.Danger.Text",
        "Theme.Critical.Background", "Theme.Critical.BackgroundStrong", "Theme.Critical.Border", "Theme.Critical.Text",

        "Theme.Button.Background", "Theme.Button.Foreground", "Theme.Button.Border",
        "Theme.Button.HoverBackground", "Theme.Button.PressedBackground",
        "Theme.Button.DisabledBackground", "Theme.Button.DisabledForeground", "Theme.Button.DisabledBorder",

        "Theme.ToolTip.Background", "Theme.ToolTip.Foreground", "Theme.ToolTip.Border",
        "Theme.ScrollBar.Track", "Theme.ScrollBar.Thumb", "Theme.ScrollBar.ThumbHover",

        "Theme.PrintPreview.PaperBackground", "Theme.PrintPreview.Text",
        "Theme.PrintPreview.MutedText", "Theme.PrintPreview.Border",
        "Theme.PrintPreview.HeaderBackground", "Theme.PrintPreview.AltRow",
        "Theme.PrintPreview.SelectedBackground", "Theme.PrintPreview.SelectedText",
        "Theme.PrintPreview.Positive", "Theme.PrintPreview.Negative",
    ];

    [Theory]
    [MemberData(nameof(RequiredRoles))]
    public void Zorunlu_semantik_rol_iki_temada_da_tanimli(string key)
    {
        var (light, dark) = Themes();

        Assert.True(light.Contains(key), $"LightTheme'de eksik: {key}");
        Assert.True(dark.Contains(key),  $"DarkTheme'de eksik: {key}");
    }

    /// <summary>
    /// Baskı katmanı temadan BAĞIMSIZDIR: iki sözlükte de aynı değeri taşımalıdır.
    /// Biri kazara "temaya uydurulursa" bu test kırılır — rapor önizlemesi
    /// yeniden temayla birlikte renk değiştirmeye başlamadan önce yakalanır.
    /// </summary>
    [Fact]
    public void PrintPreview_token_lari_iki_temada_ayni_degeri_tasir()
    {
        var (light, dark) = Themes();

        var printKeys = StringKeys(light).Where(k => k.StartsWith("Theme.PrintPreview.", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(printKeys);

        foreach (var key in printKeys)
        {
            var l = (SolidColorBrush)light[key];
            var d = (SolidColorBrush)dark[key];
            Assert.True(l.Color == d.Color,
                $"{key} temaya göre değişiyor — baskı katmanı sabit olmalı. " +
                $"Light={Contrast.Describe(l.Color)} Dark={Contrast.Describe(d.Color)}");
        }
    }

    /// <summary>
    /// Koyu tema gerçekten koyu, açık tema gerçekten açık olmalı. Kopyala-yapıştır
    /// sırasında bir yüzeyin yanlış temaya kaçmasını yakalar.
    /// </summary>
    [Theory]
    [InlineData("Theme.AppBackground")]
    [InlineData("Theme.Surface")]
    [InlineData("Theme.CardBackground")]
    [InlineData("Theme.InputBackground")]
    [InlineData("Theme.Menu.Background")]
    [InlineData("Theme.Popup.Background")]
    [InlineData("Theme.DataGridRow")]
    public void Yuzey_token_lari_dogru_yonde(string key)
    {
        var (light, dark) = Themes();

        var l = ((SolidColorBrush)light[key]).Color;
        var d = ((SolidColorBrush)dark[key]).Color;

        var white = Colors.White;
        var lightIsLight = Contrast.Ratio(l, white) < 2.0;   // beyaza yakın
        var darkIsDark   = Contrast.Ratio(d, white) > 5.0;   // beyazdan uzak

        Assert.True(lightIsLight, $"{key} açık temada koyu görünüyor: {Contrast.Describe(l)}");
        Assert.True(darkIsDark,   $"{key} koyu temada açık görünüyor: {Contrast.Describe(d)}");
    }
}
