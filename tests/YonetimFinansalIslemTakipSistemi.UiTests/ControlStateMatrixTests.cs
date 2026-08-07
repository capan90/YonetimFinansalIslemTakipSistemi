using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Kontrol × durum matrisi.
///
/// Kontrast testi token ÇİFTLERİNİ ölçer; bu test asıl kontrollerin gerçekten
/// bu token'lara bağlı olduğunu doğrular: stil uygulama zincirinden geçirilen
/// canlı kontrollerin şablonu üretilir ve görsel ağaçtaki metin/zemin renkleri
/// okunur. Yani "token doğru ama kontrol onu kullanmıyor" hatasını yakalar —
/// Faz B'nin asıl kırılma biçimi buydu.
/// </summary>
public class ControlStateMatrixTests
{
    public static TheoryData<string> ThemeNames() => [ThemeTestHost.Light, ThemeTestHost.Dark];

    /// <summary>
    /// Her kontrol için tema stili bulunmalı ve o stil WPF'in yerleşik
    /// şablonunu bırakmalı (Template setter'ı olmalı). Template'i olmayan bir
    /// stil, kontrolü Aero2'nin sabit renkli şablonuna geri düşürür.
    /// </summary>
    [Theory]
    [InlineData(typeof(System.Windows.Controls.Button))]
    [InlineData(typeof(System.Windows.Controls.TextBox))]
    [InlineData(typeof(System.Windows.Controls.PasswordBox))]
    [InlineData(typeof(System.Windows.Controls.ComboBox))]
    [InlineData(typeof(System.Windows.Controls.ComboBoxItem))]
    [InlineData(typeof(System.Windows.Controls.MenuItem))]
    [InlineData(typeof(System.Windows.Controls.Menu))]
    [InlineData(typeof(System.Windows.Controls.ContextMenu))]
    [InlineData(typeof(System.Windows.Controls.CheckBox))]
    [InlineData(typeof(System.Windows.Controls.RadioButton))]
    [InlineData(typeof(System.Windows.Controls.Label))]
    [InlineData(typeof(System.Windows.Controls.GroupBox))]
    [InlineData(typeof(System.Windows.Controls.DatePicker))]
    [InlineData(typeof(System.Windows.Controls.Primitives.DatePickerTextBox))]
    [InlineData(typeof(System.Windows.Controls.ListBoxItem))]
    [InlineData(typeof(System.Windows.Controls.ProgressBar))]
    [InlineData(typeof(System.Windows.Controls.ToolTip))]
    [InlineData(typeof(System.Windows.Controls.Primitives.ScrollBar))]
    [InlineData(typeof(System.Windows.Controls.Separator))]
    // Kabuk sekme şeridi (Faz D5). Aero2'nin hazır TabItem şablonu sabit
    // degrade + sabit koyu yazı kullanır; koyu temada şerit açık gri kalırdı.
    [InlineData(typeof(System.Windows.Controls.TabControl))]
    [InlineData(typeof(System.Windows.Controls.TabItem))]
    public void Kontrolun_ortuk_stili_ve_kendi_sablonu_var(Type controlType)
    {
        ThemeTestHost.Run(() =>
        {
            var style = WpfApp.Current.TryFindResource(controlType) as Style;
            Assert.True(style is not null, $"{controlType.Name} için örtük stil tanımlı değil.");

            var hasTemplate = HasSetter(style!, Control.TemplateProperty);
            Assert.True(hasTemplate,
                $"{controlType.Name} stilinde Template yok — WPF'in tema körü yerleşik şablonuna düşer.");
        });
    }

    /// <summary>
    /// Durum matrisi: her (kontrol, durum) için metin ve zemin token'ı ayrı ayrı
    /// tanımlı, farklı ve kontrastlı olmalı.
    /// </summary>
    public sealed record StateRow(string Control, string State, string Foreground, string Background, double Threshold);

    private static readonly StateRow[] Matrix =
    [
        // Primary buton
        new("PrimaryButton",   "Normal",   "Theme.OnPrimary",   "Theme.Primary",             Contrast.AA),
        new("PrimaryButton",   "Hover",    "Theme.OnPrimary",   "Theme.PrimaryHover",        Contrast.AA),
        new("PrimaryButton",   "Pressed",  "Theme.OnPrimary",   "Theme.PrimaryPressed",      Contrast.AA),
        new("PrimaryButton",   "Disabled", "Theme.OnDisabled",  "Theme.DisabledBackground",  Contrast.AA),

        // Secondary buton
        new("SecondaryButton", "Normal",   "Theme.OnSecondary", "Theme.Secondary",           Contrast.AA),
        new("SecondaryButton", "Hover",    "Theme.OnSecondary", "Theme.SecondaryHover",      Contrast.AA),
        new("SecondaryButton", "Pressed",  "Theme.OnSecondary", "Theme.SecondaryPressed",    Contrast.AA),
        new("SecondaryButton", "Disabled", "Theme.OnDisabled",  "Theme.DisabledBackground",  Contrast.AA),

        // Danger buton
        new("DangerButton",    "Normal",   "Theme.OnDanger",    "Theme.Danger",              Contrast.AA),
        new("DangerButton",    "Hover",    "Theme.OnDanger",    "Theme.DangerHover",         Contrast.AA),
        new("DangerButton",    "Pressed",  "Theme.OnDanger",    "Theme.DangerPressed",       Contrast.AA),
        new("DangerButton",    "Disabled", "Theme.OnDisabled",  "Theme.DisabledBackground",  Contrast.AA),

        // Nötr (varsayılan) buton
        new("Button",          "Normal",   "Theme.Button.Foreground",         "Theme.Button.Background",         Contrast.AA),
        new("Button",          "Hover",    "Theme.Button.Foreground",         "Theme.Button.HoverBackground",    Contrast.AA),
        new("Button",          "Pressed",  "Theme.Button.Foreground",         "Theme.Button.PressedBackground",  Contrast.AA),
        new("Button",          "Disabled", "Theme.Button.DisabledForeground", "Theme.Button.DisabledBackground", Contrast.AALarge),

        // MenuItem
        new("MenuItem",        "Normal",   "Theme.Menu.Foreground",         "Theme.Menu.Background",         Contrast.AA),
        new("MenuItem",        "Hover",    "Theme.Menu.HoverForeground",    "Theme.Menu.HoverBackground",    Contrast.AA),
        new("MenuItem",        "Selected", "Theme.Menu.SelectedForeground", "Theme.Menu.SelectedBackground", Contrast.AA),
        new("MenuItem",        "Disabled", "Theme.Menu.DisabledForeground", "Theme.Menu.Background",         Contrast.AALarge),
        new("MenuItem",        "Popup",    "Theme.Popup.Foreground",        "Theme.Popup.Background",        Contrast.AA),

        // TextBox
        new("TextBox",         "Normal",         "Theme.Text",                      "Theme.InputBackground",           Contrast.AA),
        new("TextBox",         "Disabled",       "Theme.Input.DisabledForeground",  "Theme.Input.DisabledBackground",  Contrast.AALarge),
        new("TextBox",         "ReadOnly",       "Theme.Text",                      "Theme.Input.ReadOnlyBackground",  Contrast.AA),
        new("TextBox",         "Selected",       "Theme.Input.SelectionForeground", "Theme.Input.SelectionBackground", Contrast.AA),
        new("TextBox",         "Placeholder",    "Theme.Input.Placeholder",         "Theme.InputBackground",           Contrast.AALarge),
        // Odak halkası bir GRAFİK bileşenidir; eşik 3:1
        new("TextBox",         "Focused",        "Theme.InputBorderFocus",          "Theme.InputBackground",           Contrast.AALarge),
        new("TextBox",         "ValidationError","Theme.Danger",                    "Theme.InputBackground",           Contrast.AALarge),

        // ComboBox
        new("ComboBox",        "Normal",   "Theme.Text",                            "Theme.InputBackground",                 Contrast.AA),
        new("ComboBox",        "Dropdown", "Theme.ComboBox.DropdownForeground",     "Theme.ComboBox.DropdownBackground",     Contrast.AA),
        new("ComboBox",        "Hover",    "Theme.ComboBox.ItemHoverForeground",    "Theme.ComboBox.ItemHoverBackground",    Contrast.AA),
        new("ComboBox",        "Selected", "Theme.ComboBox.ItemSelectedForeground", "Theme.ComboBox.ItemSelectedBackground", Contrast.AA),
        new("ComboBox",        "Disabled", "Theme.Input.DisabledForeground",        "Theme.Input.DisabledBackground",        Contrast.AALarge),

        // DataGridRow
        new("DataGridRow",     "Normal",   "Theme.Text",                 "Theme.DataGridRow",         Contrast.AA),
        new("DataGridRow",     "Alt",      "Theme.Text",                 "Theme.DataGridAltRow",      Contrast.AA),
        new("DataGridRow",     "Hover",    "Theme.Text",                 "Theme.DataGridHoverRow",    Contrast.AA),
        new("DataGridRow",     "Selected", "Theme.DataGridSelectedText", "Theme.DataGridSelectedRow", Contrast.AA),

        // CheckBox — işaret, kutu dolgusunun üzerinde durur
        new("CheckBox",        "Normal",   "Theme.Text",                     "Theme.InputBackground",          Contrast.AA),
        new("CheckBox",        "Checked",  "Theme.OnPrimary",                "Theme.Primary",                  Contrast.AA),
        new("CheckBox",        "Disabled", "Theme.Input.DisabledForeground", "Theme.Input.DisabledBackground", Contrast.AALarge),

        // Navigasyon butonu
        new("NavButton",       "Normal",   "Theme.Nav.Foreground",            "Theme.Nav.ItemBackground",        Contrast.AA),
        new("NavButton",       "Hover",    "Theme.Nav.HoverForeground",       "Theme.Nav.HoverBackground",       Contrast.AA),
        new("NavButton",       "Pressed",  "Theme.Nav.ActiveForeground",      "Theme.Nav.ActiveBackground",      Contrast.AA),
        new("NavButton",       "Disabled", "Theme.Button.DisabledForeground", "Theme.Button.DisabledBackground", Contrast.AALarge),

        // Navigasyon rayı listesi (Faz D7.1). Ray HER İKİ TEMADA DA koyu bir
        // yüzey; öğeler örtük ListBoxItem stilinden Theme.Text alıyordu ve
        // açık temada 1,40:1 veriyordu. Artık Nav token ailesini okuyorlar.
        new("NavListItem",     "Normal",   "Theme.Nav.Foreground",       "Theme.Nav.Background",       Contrast.AA),
        new("NavListItem",     "Hover",    "Theme.Nav.HoverForeground",  "Theme.Nav.HoverBackground",  Contrast.AA),
        new("NavListItem",     "Selected", "Theme.Nav.ActiveForeground", "Theme.Nav.ActiveBackground", Contrast.AA),

        // Grup başlıkları da aynı zeminde durur
        new("NavGroupHeader",  "Normal",   "Theme.Nav.Foreground",       "Theme.Nav.Background",       Contrast.AA),

        // Kabuk sekmesi (Faz D5). Seçili sekme ile içerik alanı BİLEREK aynı
        // yüzeyde (Theme.SurfaceSubtle) durur — kabuk ile ekran tek parça
        // okunsun diye. Ayrımı taşıyan 2px Theme.Primary şeridi grafik bir
        // bileşendir, eşiği 3:1.
        new("TabItem",         "Normal",   "Theme.MutedText",           "Theme.SurfaceSubtle",     Contrast.AA),
        new("TabItem",         "Hover",    "Theme.Text",                "Theme.DataGridHoverRow",  Contrast.AA),
        new("TabItem",         "Selected", "Theme.Text",                "Theme.SurfaceSubtle",     Contrast.AA),
        new("TabItem",         "Marker",   "Theme.Primary",             "Theme.SurfaceSubtle",     Contrast.AALarge),
        new("TabItem",         "Disabled", "Theme.Button.DisabledForeground", "Theme.SurfaceSubtle", Contrast.AALarge),
    ];

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Durum_matrisi_her_temada_okunabilir(string themeName)
    {
        var theme = ThemeTestHost.LoadTheme(themeName);
        var failures = new List<string>();

        foreach (var row in Matrix)
        {
            var fg = Resolve(theme, row.Foreground);
            var bg = Resolve(theme, row.Background);

            if (fg is null) { failures.Add($"{row.Control}/{row.State}: '{row.Foreground}' yok"); continue; }
            if (bg is null) { failures.Add($"{row.Control}/{row.State}: '{row.Background}' yok"); continue; }

            if (fg.Value == bg.Value)
            {
                failures.Add($"{row.Control}/{row.State}: metin ve zemin aynı renk ({Contrast.Describe(fg.Value)})");
                continue;
            }

            var ratio = Contrast.Ratio(fg.Value, bg.Value);
            if (ratio < row.Threshold)
                failures.Add($"{row.Control}/{row.State}: {ratio:F2}:1 < {row.Threshold:F1}:1 " +
                             $"({Contrast.Describe(fg.Value)} / {Contrast.Describe(bg.Value)})");
        }

        Assert.True(failures.Count == 0,
            $"{themeName} — {failures.Count} durum ihlali:{Environment.NewLine}" +
            string.Join(Environment.NewLine, failures));
    }

    /// <summary>
    /// Canlı kontrol testi: kontrol gerçekten oluşturulur, şablonu uygulanır ve
    /// çözülen Foreground/Background renkleri karşılaştırılır. Bir kontrolün
    /// stili varken şablonunun renkleri yine de sabit kalmışsa burada görünür.
    /// </summary>
    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Canli_kontroller_temayla_birlikte_renk_degistirir(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);

        ThemeTestHost.Run(() =>
        {
            var failures = new List<string>();

            foreach (var (name, factory) in LiveControls())
            {
                var control = factory();
                // Şablonu üret: Measure çağrısı ApplyTemplate'i tetikler
                control.Measure(new Size(300, 100));

                var fg = control.GetValue(Control.ForegroundProperty) as SolidColorBrush;
                var bg = control.GetValue(Control.BackgroundProperty) as SolidColorBrush;

                if (fg is null)
                {
                    failures.Add($"{name}: Foreground bir SolidColorBrush'a çözülmedi");
                    continue;
                }

                // Zemin şeffaf olabilir (MenuItem gibi) — o durumda metin rengi
                // kapsayıcı yüzeye göre ayrıca kontrast testinde ölçülür.
                if (bg is null || bg.Color.A == 0) continue;

                if (fg.Color == bg.Color)
                    failures.Add($"{name}: Foreground ve Background aynı ({Contrast.Describe(fg.Color)})");
            }

            Assert.True(failures.Count == 0,
                $"{themeName}:{Environment.NewLine}" + string.Join(Environment.NewLine, failures));
        });
    }

    private static IEnumerable<(string, Func<Control>)> LiveControls() =>
    [
        ("Button",      () => new System.Windows.Controls.Button { Content = "Test" }),
        ("TextBox",     () => new System.Windows.Controls.TextBox { Text = "Test" }),
        ("PasswordBox", () => new System.Windows.Controls.PasswordBox()),
        ("ComboBox",    () => new System.Windows.Controls.ComboBox()),
        ("CheckBox",    () => new System.Windows.Controls.CheckBox { Content = "Test" }),
        ("RadioButton", () => new System.Windows.Controls.RadioButton { Content = "Test" }),
        ("Label",       () => new System.Windows.Controls.Label { Content = "Test" }),
        ("GroupBox",    () => new System.Windows.Controls.GroupBox { Header = "Test" }),
        ("DatePicker",  () => new System.Windows.Controls.DatePicker()),
        ("ProgressBar", () => new System.Windows.Controls.ProgressBar()),
        ("Menu",        () => new System.Windows.Controls.Menu()),
    ];

    private static bool HasSetter(Style style, DependencyProperty property)
    {
        for (var s = style; s is not null; s = s.BasedOn)
            if (s.Setters.OfType<Setter>().Any(x => x.Property == property))
                return true;
        return false;
    }

    private static Color? Resolve(ResourceDictionary dict, string key) =>
        dict.Contains(key) && dict[key] is SolidColorBrush b ? b.Color : null;
}
