using System.Windows;
using System.Windows.Media;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Semantik metin/zemin çiftlerinin okunabilirliği — her iki temada.
///
/// Bu testin varlık sebebi: Faz B'de token'lar vardı ama HANGİ metnin HANGİ
/// zeminin üzerinde durduğu hiçbir yerde yazılı değildi. Aşağıdaki tablo o
/// eşleşmeyi açıkça kaydeder; bir token değeri değişirse eşi de birlikte
/// düşünülmek zorunda kalır.
/// </summary>
public class ThemeContrastTests
{
    /// <summary>metin token'ı, zemin token'ı, eşik, açıklama.</summary>
    public sealed record Pair(string Foreground, string Background, double Threshold, string Description);

    private static readonly Pair[] Pairs =
    [
        // ── Gövde ve yüzeyler ────────────────────────────────────────────────
        new("Theme.Text",       "Theme.AppBackground", Contrast.AA, "Gövde metni / uygulama zemini"),
        new("Theme.Text",       "Theme.Surface",       Contrast.AA, "Gövde metni / kart yüzeyi"),
        new("Theme.Text",       "Theme.SurfaceAlt",    Contrast.AA, "Gövde metni / ikincil yüzey"),
        new("Theme.Text",       "Theme.SurfaceSubtle", Contrast.AA, "Gövde metni / soluk yüzey"),
        new("Theme.LabelText",  "Theme.Surface",       Contrast.AA, "Etiket metni / kart yüzeyi"),
        new("Theme.LabelText",  "Theme.AppBackground", Contrast.AA, "Etiket metni / uygulama zemini"),
        new("Theme.MutedText",  "Theme.Surface",       Contrast.AA, "Soluk metin / kart yüzeyi"),
        new("Theme.MutedText",  "Theme.AppBackground", Contrast.AA, "Soluk metin / uygulama zemini"),

        // ── Menü ─────────────────────────────────────────────────────────────
        new("Theme.Menu.Foreground",         "Theme.Menu.Background",         Contrast.AA, "Menü metni / menü zemini"),
        new("Theme.Menu.HoverForeground",    "Theme.Menu.HoverBackground",    Contrast.AA, "Menü hover metni / hover zemini"),
        new("Theme.Menu.SelectedForeground", "Theme.Menu.SelectedBackground", Contrast.AA, "Açık menü başlığı / seçili zemin"),
        new("Theme.Menu.DisabledForeground", "Theme.Menu.Background",         Contrast.AALarge, "Devre dışı menü öğesi / menü zemini"),

        // ── Popup / açılır liste ─────────────────────────────────────────────
        new("Theme.Popup.Foreground",        "Theme.Popup.Background",        Contrast.AA, "Popup metni / popup zemini"),
        new("Theme.Menu.HoverForeground",    "Theme.Menu.HoverBackground",    Contrast.AA, "Popup öğesi hover"),

        // ── Metin girişi ─────────────────────────────────────────────────────
        new("Theme.Text",                     "Theme.InputBackground",          Contrast.AA,      "TextBox metni / TextBox zemini"),
        new("Theme.Input.Placeholder",        "Theme.InputBackground",          Contrast.AALarge, "Placeholder / TextBox zemini"),
        new("Theme.Input.DisabledForeground", "Theme.Input.DisabledBackground", Contrast.AALarge, "Devre dışı input metni / devre dışı zemin"),
        new("Theme.Text",                     "Theme.Input.ReadOnlyBackground", Contrast.AA,      "Salt okunur input metni / zemini"),
        new("Theme.Input.SelectionForeground","Theme.Input.SelectionBackground",Contrast.AA,      "Seçili metin / seçim zemini"),

        // ── ComboBox ─────────────────────────────────────────────────────────
        new("Theme.Text",                              "Theme.InputBackground",                  Contrast.AA, "ComboBox seçili değeri / kapalı zemin"),
        new("Theme.ComboBox.DropdownForeground",       "Theme.ComboBox.DropdownBackground",      Contrast.AA, "ComboBox item metni / açılır liste zemini"),
        new("Theme.ComboBox.ItemHoverForeground",      "Theme.ComboBox.ItemHoverBackground",     Contrast.AA, "ComboBox item hover metni / hover zemini"),
        new("Theme.ComboBox.ItemSelectedForeground",   "Theme.ComboBox.ItemSelectedBackground",  Contrast.AA, "Seçili ComboBox item metni / seçili zemin"),

        // ── DataGrid ─────────────────────────────────────────────────────────
        new("Theme.Text",                  "Theme.DataGridRow",         Contrast.AA, "DataGrid metni / satır zemini"),
        new("Theme.Text",                  "Theme.DataGridAltRow",      Contrast.AA, "DataGrid metni / alternatif satır"),
        new("Theme.DataGridSelectedText",  "Theme.DataGridSelectedRow", Contrast.AA, "DataGrid seçili metni / seçili zemin"),
        new("Theme.Text",                  "Theme.DataGridHoverRow",    Contrast.AA, "DataGrid metni / hover satırı"),
        new("Theme.DataGridHeaderText",    "Theme.DataGridHeader",      Contrast.AA, "Sütun başlığı / başlık zemini"),

        // ── Navigasyon şeridi ────────────────────────────────────────────────
        new("Theme.Nav.Foreground",       "Theme.Nav.Background",       Contrast.AA, "Navigasyon metni / şerit zemini"),
        new("Theme.Nav.Foreground",       "Theme.Nav.ItemBackground",   Contrast.AA, "Navigasyon öğesi metni / öğe zemini"),
        new("Theme.Nav.HoverForeground",  "Theme.Nav.HoverBackground",  Contrast.AA, "Navigasyon hover metni / hover zemini"),
        new("Theme.Nav.ActiveForeground", "Theme.Nav.ActiveBackground", Contrast.AA, "Aktif navigasyon metni / aktif zemin"),

        // ── Durum (Info / Warning / Error / Critical) ────────────────────────
        new("Theme.Info.Text",     "Theme.Info.Background",           Contrast.AA, "Info metni / info paneli"),
        new("Theme.Info.Text",     "Theme.Info.BackgroundStrong",     Contrast.AA, "Info rozet metni / rozet dolgusu"),
        new("Theme.Warning.Text",  "Theme.Warning.Background",        Contrast.AA, "Uyarı metni / uyarı paneli"),
        new("Theme.Warning.Text",  "Theme.Warning.BackgroundStrong",  Contrast.AA, "Uyarı rozet metni / rozet dolgusu"),
        new("Theme.Danger.Text",   "Theme.Danger.Background",         Contrast.AA, "Hata metni / hata paneli"),
        new("Theme.Danger.Text",   "Theme.Danger.BackgroundStrong",   Contrast.AA, "Hata rozet metni / rozet dolgusu"),
        new("Theme.Critical.Text", "Theme.Critical.BackgroundStrong", Contrast.AA, "Kritik rozet metni / dolu kritik zemin"),
        new("Theme.Accent.Text",   "Theme.Accent.Background",         Contrast.AA, "Vurgu metni / vurgu paneli"),
        new("Theme.Success.Text",  "Theme.Success.Background",        Contrast.AA, "Başarı metni / başarı paneli"),

        // Durum renklerinin düz yüzey üzerindeki kullanımı (rakam/etiket)
        new("Theme.Danger",  "Theme.Surface", Contrast.AA, "Tehlike rengi / kart yüzeyi"),
        new("Theme.Success", "Theme.Surface", Contrast.AALarge, "Başarı rengi / kart yüzeyi (büyük rakam)"),
        new("Theme.Warning", "Theme.Surface", Contrast.AALarge, "Uyarı rengi / kart yüzeyi (büyük rakam)"),

        // ── Butonlar ─────────────────────────────────────────────────────────
        new("Theme.OnPrimary",   "Theme.Primary",        Contrast.AA, "Birincil buton metni / zemini"),
        new("Theme.OnPrimary",   "Theme.PrimaryHover",   Contrast.AA, "Birincil buton hover"),
        new("Theme.OnPrimary",   "Theme.PrimaryPressed", Contrast.AA, "Birincil buton basılı"),
        new("Theme.OnSecondary", "Theme.Secondary",        Contrast.AA, "İkincil buton metni / zemini"),
        new("Theme.OnSecondary", "Theme.SecondaryHover",   Contrast.AA, "İkincil buton hover"),
        new("Theme.OnSecondary", "Theme.SecondaryPressed", Contrast.AA, "İkincil buton basılı"),
        new("Theme.OnDanger",    "Theme.Danger",        Contrast.AA, "Tehlike butonu metni / zemini"),
        new("Theme.OnDanger",    "Theme.DangerHover",   Contrast.AA, "Tehlike butonu hover"),
        new("Theme.OnDanger",    "Theme.DangerPressed", Contrast.AA, "Tehlike butonu basılı"),
        new("Theme.OnDisabled",  "Theme.DisabledBackground", Contrast.AA, "Devre dışı buton metni / zemini"),

        new("Theme.Button.Foreground",         "Theme.Button.Background",         Contrast.AA, "Nötr buton metni / zemini"),
        new("Theme.Button.Foreground",         "Theme.Button.HoverBackground",    Contrast.AA, "Nötr buton hover"),
        new("Theme.Button.Foreground",         "Theme.Button.PressedBackground",  Contrast.AA, "Nötr buton basılı"),
        new("Theme.Button.DisabledForeground", "Theme.Button.DisabledBackground", Contrast.AALarge, "Devre dışı nötr buton"),

        // ── ToolTip ──────────────────────────────────────────────────────────
        new("Theme.ToolTip.Foreground", "Theme.ToolTip.Background", Contrast.AA, "Tooltip metni / tooltip zemini"),

        // ── Baskı katmanı (rapor önizleme) ───────────────────────────────────
        new("Theme.PrintPreview.Text",         "Theme.PrintPreview.PaperBackground",  Contrast.AA, "Rapor metni / kâğıt"),
        new("Theme.PrintPreview.MutedText",    "Theme.PrintPreview.PaperBackground",  Contrast.AA, "Rapor ikincil metni / kâğıt"),
        new("Theme.PrintPreview.Text",         "Theme.PrintPreview.HeaderBackground", Contrast.AA, "Rapor sütun başlığı / başlık zemini"),
        new("Theme.PrintPreview.Text",         "Theme.PrintPreview.AltRow",           Contrast.AA, "Rapor metni / alternatif satır"),
        new("Theme.PrintPreview.SelectedText", "Theme.PrintPreview.SelectedBackground", Contrast.AA, "Rapor seçili satır"),
        new("Theme.PrintPreview.Positive",     "Theme.PrintPreview.PaperBackground",  Contrast.AA, "Rapor giriş rakamı / kâğıt"),
        new("Theme.PrintPreview.Negative",     "Theme.PrintPreview.PaperBackground",  Contrast.AA, "Rapor çıkış rakamı / kâğıt"),
    ];

    public static TheoryData<string> ThemeNames() => [ThemeTestHost.Light, ThemeTestHost.Dark];

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Semantik_ciftler_okunabilir(string themeName)
    {
        var theme = ThemeTestHost.LoadTheme(themeName);
        var failures = new List<string>();

        foreach (var pair in Pairs)
        {
            var fg = Resolve(theme, pair.Foreground);
            var bg = Resolve(theme, pair.Background);

            if (fg is null) { failures.Add($"{themeName}: '{pair.Foreground}' tanımlı değil"); continue; }
            if (bg is null) { failures.Add($"{themeName}: '{pair.Background}' tanımlı değil"); continue; }

            var ratio = Contrast.Ratio(fg.Value, bg.Value);
            if (ratio < pair.Threshold)
            {
                failures.Add(
                    $"{themeName}: {pair.Description} — {pair.Foreground} {Contrast.Describe(fg.Value)} / " +
                    $"{pair.Background} {Contrast.Describe(bg.Value)} = {ratio:F2}:1 " +
                    $"(hedef {pair.Threshold:F1}:1)");
            }
        }

        Assert.True(failures.Count == 0,
            $"{failures.Count} kontrast ihlali:{Environment.NewLine}" + string.Join(Environment.NewLine, failures));
    }

    /// <summary>
    /// Metin ve zemin token'ı AYNI renge çözülmemeli. Kontrast eşiğinden ayrı
    /// tutulur çünkü bu hata sınıfı ("beyaz metin beyaz zeminde") en sık
    /// kopyala-yapıştırla oluşur ve mesajı net olmalı.
    /// </summary>
    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Metin_ve_zemin_ayni_renge_cozulmez(string themeName)
    {
        var theme = ThemeTestHost.LoadTheme(themeName);

        foreach (var pair in Pairs)
        {
            var fg = Resolve(theme, pair.Foreground);
            var bg = Resolve(theme, pair.Background);
            if (fg is null || bg is null) continue;

            Assert.True(fg.Value != bg.Value,
                $"{themeName}: {pair.Description} — metin ve zemin aynı renk ({Contrast.Describe(fg.Value)})");
        }
    }

    private static Color? Resolve(ResourceDictionary dict, string key) =>
        dict.Contains(key) && dict[key] is SolidColorBrush b ? b.Color : null;
}
