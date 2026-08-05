using System.Text;
using System.Text.RegularExpressions;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Stil zinciri denetimi — sessiz tema kaybının en sinsi biçimi.
///
/// WPF'te bir kontrole BasedOn'suz bir Style verilirse, kontrol uygulamanın
/// örtük tema stilini TAMAMEN kaybeder ve yerleşik (Aero2) stiline düşer.
/// O stilin renkleri sabit hex'tir ve tema sözlüğünü hiç görmez. Belirti,
/// "bir pencerede kontroller temaya uymuyor" şeklinde ortaya çıkar ve token
/// taramasıyla YAKALANMAZ — çünkü ortada yanlış token yoktur, hiç token yoktur.
///
/// Faz B düzeltmesinde bu yolla bulunanlar: MailSettings input/buton stilleri,
/// dört Excel sihirbazının DataGridRow stili, CargoOperationCenter kart stili,
/// SystemHealth bölüm kutusu, ReportWindow GroupBox ve MainWindow'un kod
/// tarafında ürettiği DataGridColumnHeader stili.
/// </summary>
public class StyleChainTests
{
    /// <summary>
    /// Örtük tema stili tanımlı olan tipler. Bunlardan birini hedefleyen yerel
    /// bir stil, BasedOn ile zincire bağlanmak zorundadır.
    /// </summary>
    private static readonly string[] ThemedTypes =
    [
        "Button", "TextBox", "PasswordBox", "ComboBox", "ComboBoxItem",
        "CheckBox", "RadioButton", "DatePicker", "DatePickerTextBox",
        "Label", "GroupBox", "Menu", "MenuItem", "ContextMenu",
        "ListBox", "ListBoxItem", "ProgressBar", "Separator", "ToolTip",
        "DataGrid", "DataGridRow", "DataGridCell", "DataGridColumnHeader",
        "ScrollBar", "Calendar", "CalendarDayButton", "CalendarItem", "Window",
    ];

    [Fact]
    public void Pencerelerde_tanimli_stiller_tema_zincirine_bagli()
    {
        var typeAlternation = string.Join("|", ThemedTypes);
        var styleTag = new Regex(
            $@"<Style\b(?<attrs>[^>]*?)TargetType=""(?:\{{x:Type\s+)?(?<type>{typeAlternation})\}}?""(?<rest>[^>]*)>",
            RegexOptions.Compiled);

        var violations = new List<string>();

        foreach (var file in UiSourceLocator.XamlFiles(includeResources: true))
        {
            var relative = UiSourceLocator.Relative(file);

            // Resources/ altındaki sözlükler zincirin KENDİSİDİR; onlar muaftır.
            if (relative.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase)) continue;

            var lines = File.ReadAllLines(file, Encoding.UTF8);
            for (var i = 0; i < lines.Length; i++)
            {
                foreach (Match m in styleTag.Matches(lines[i]))
                {
                    var whole = m.Groups["attrs"].Value + m.Groups["rest"].Value;
                    if (whole.Contains("BasedOn", StringComparison.Ordinal)) continue;

                    violations.Add(
                        $"{relative}:{i + 1}  TargetType={m.Groups["type"].Value} — BasedOn yok, " +
                        "kontrol WPF'in tema körü yerleşik stiline düşer.");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"{violations.Count} stil tema zincirinden kopuk:{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Kod tarafında üretilen stiller: <c>new Style(typeof(X))</c> tek argümanlı
    /// biçimde BasedOn taşımaz. İkinci argüman (temel stil) verilmelidir.
    /// </summary>
    [Fact]
    public void Kod_tarafinda_uretilen_stiller_temel_stil_aliyor()
    {
        // "new Style(typeof(Foo))" — parantez içinde virgül YOK
        var singleArg = new Regex(@"new\s+Style\s*\(\s*typeof\s*\([^)]*\)\s*\)", RegexOptions.Compiled);

        var violations = new List<string>();

        foreach (var file in UiSourceLocator.CsFiles())
        {
            var relative = UiSourceLocator.Relative(file);
            var lines    = File.ReadAllLines(file, Encoding.UTF8);

            for (var i = 0; i < lines.Length; i++)
            {
                if (!singleArg.IsMatch(lines[i])) continue;
                violations.Add($"{relative}:{i + 1}  {lines[i].Trim()}");
            }
        }

        Assert.True(violations.Count == 0,
            "BasedOn'suz kod stili kontrolü temadan koparır; " +
            $"new Style(typeof(X), TryFindResource(typeof(X)) as Style) kullanın:{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations));
    }
}
