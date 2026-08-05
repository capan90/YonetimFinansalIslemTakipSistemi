using System.Text;
using System.Text.RegularExpressions;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Ham renk taraması — token disiplininin nöbetçisi.
///
/// Statik tarama tek başına yeterli değildir (bu yüzden kontrast ve parse
/// testleri de var) ama geri kaymayı erken yakalar: XAML'de yeniden hex
/// belirmesi ya da code-behind'da sabit fırça atanması.
/// </summary>
public class RawColorScanTests
{
    private static readonly Regex HexColor = new(
        @"=""\s*#(?:[0-9A-Fa-f]{3,4}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})\s*""",
        RegexOptions.Compiled);

    /// <summary>Renk taşıyan özniteliklerde adlandırılmış sistem rengi kullanımı.</summary>
    private static readonly Regex NamedSystemColor = new(
        @"\b(?:Foreground|Background|BorderBrush|Fill|Stroke|CaretBrush|SelectionBrush|SelectionTextBrush|HorizontalGridLinesBrush|VerticalGridLinesBrush|RowBackground|AlternatingRowBackground)=""(?<value>[A-Za-z]+)""",
        RegexOptions.Compiled);

    /// <summary>Şeffaflık bir renk değil, "yok" demektir; token'a bağlanmaz.</summary>
    private static readonly HashSet<string> AllowedNamedValues = new(StringComparer.Ordinal) { "Transparent" };

    [Fact]
    public void Pencere_ve_stil_XAML_lerinde_ham_hex_renk_yok()
    {
        var violations = new List<string>();

        foreach (var file in UiSourceLocator.XamlFiles(includeResources: true))
        {
            // Tema sözlükleri ham hex tanımlamak zorundadır — kaynağın kendisi orasıdır.
            if (UiSourceLocator.IsThemeDictionary(file)) continue;

            var lines = File.ReadAllLines(file, Encoding.UTF8);
            for (var i = 0; i < lines.Length; i++)
            {
                if (HexColor.IsMatch(lines[i]))
                    violations.Add($"{UiSourceLocator.Relative(file)}:{i + 1}  {lines[i].Trim()}");
            }
        }

        Assert.True(violations.Count == 0,
            $"XAML'de {violations.Count} ham hex renk:{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Pencere_ve_stil_XAML_lerinde_adlandirilmis_sistem_rengi_yok()
    {
        var violations = new List<string>();

        foreach (var file in UiSourceLocator.XamlFiles(includeResources: true))
        {
            if (UiSourceLocator.IsThemeDictionary(file)) continue;

            var lines = File.ReadAllLines(file, Encoding.UTF8);
            for (var i = 0; i < lines.Length; i++)
            {
                foreach (Match m in NamedSystemColor.Matches(lines[i]))
                {
                    var value = m.Groups["value"].Value;
                    if (AllowedNamedValues.Contains(value)) continue;
                    violations.Add($"{UiSourceLocator.Relative(file)}:{i + 1}  {m.Value}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"XAML'de {violations.Count} adlandırılmış sabit renk (White/Black/Gray...):{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Code-behind'da sabit fırça üretimi. Bunlar tema değişimini GÖRMEZ:
    /// atandıkları anda donarlar. Doğru karşılığı ThemeBrush.Apply'dır.
    /// </summary>
    [Fact]
    public void Code_behind_de_sabit_firca_uretilmiyor()
    {
        var patterns = new (Regex Rx, string Reason)[]
        {
            (new(@"\bBrushes\.(?!Transparent\b)[A-Z]\w+", RegexOptions.Compiled),
                "Brushes.* sabit sistem rengi"),
            (new(@"\bColor\.From(?:Rgb|Argb)\s*\(", RegexOptions.Compiled),
                "Color.FromRgb/FromArgb ile elle renk"),
            (new(@"\bColorConverter\.ConvertFromString\s*\(", RegexOptions.Compiled),
                "hex string'den renk üretimi"),
        };

        var violations = new List<string>();

        foreach (var file in UiSourceLocator.CsFiles())
        {
            var relative = UiSourceLocator.Relative(file);
            var lines    = File.ReadAllLines(file, Encoding.UTF8);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Yorum satırları serbest — kaldırılan desenler orada anlatılıyor
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("///") || trimmed.StartsWith("*")) continue;

                foreach (var (rx, reason) in patterns)
                {
                    if (!rx.IsMatch(line)) continue;
                    if (IsKnownException(relative)) continue;
                    violations.Add($"{relative}:{i + 1}  {reason} — {trimmed}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Code-behind'da {violations.Count} sabit renk:{Environment.NewLine}" +
            string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Bilinçli istisnalar. Her biri gerekçesiyle burada durur; listeye ekleme
    /// yapmak bir karar olmalı, kaza olmamalı.
    /// </summary>
    private static bool IsKnownException(string relativePath) => relativePath switch
    {
        // Diyalog başlık bandı: dört diyalog tipini ayıran marka renkleridir,
        // yüzey değil. İki temada da doygun kalır, üzerindeki metin Theme.OnSecondary.
        "Dialogs/MessageDialog.xaml.cs" => true,

        // Kargo dashboard grafik barları: renk Application katmanındaki DTO'dan
        // (CargoDashboardChartItem.Color) gelir. Grafik paleti Faz C'nin konusu;
        // bu fazda Application katmanına dokunulmuyor.
        "Views/Cargo/CargoDashboardWindow.xaml.cs" => true,

        // Tema fırçası çözücünün kendi yedek değeri.
        "Common/ThemeBrush.cs" => true,

        _ => false
    };

    /// <summary>
    /// Fırça DÖNDÜREN converter yeniden eklenirse yakalar: converter bağlama
    /// başına bir kez çalışır, sonucu tema değişiminde güncellenmez.
    /// </summary>
    [Fact]
    public void Converter_lar_Brush_dondurmuyor()
    {
        var converterDir = Path.Combine(UiSourceLocator.UiProjectDirectory, "Converters");
        if (!Directory.Exists(converterDir)) return;

        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(converterDir, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file, Encoding.UTF8);

            // [ValueConversion(..., typeof(Brush))] veya Brush döndüren imza
            if (Regex.IsMatch(source, @"typeof\(\s*Brush\s*\)") ||
                Regex.IsMatch(source, @"^\s*(?:public|private|internal)\s+(?:static\s+)?Brush\s+\w+", RegexOptions.Multiline))
            {
                violations.Add(UiSourceLocator.Relative(file));
            }
        }

        Assert.True(violations.Count == 0,
            "Fırça döndüren converter tema değişimini görmez; DataTrigger + DynamicResource kullanın: " +
            string.Join(", ", violations));
    }
}
