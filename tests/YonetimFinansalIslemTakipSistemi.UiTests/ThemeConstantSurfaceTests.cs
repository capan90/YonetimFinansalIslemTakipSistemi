using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// TEMA-SABİT YÜZEYLER ve gövde metni (Faz E7).
///
/// D7.1'de kullanıcının bildirdiği hatanın genel biçimi şudur:
///
///   Uygulamadaki yüzeylerin çoğu temayla birlikte DÖNER — açık temada açık,
///   koyu temada koyudur. Gövde metni tokenı (Theme.Text) da döner, bu yüzden
///   ikisi her temada birbirine uyar.
///
///   Ama bazı yüzeyler BİLEREK dönmez: navigasyon rayı her iki temada da koyu
///   lacivert, tehlike şeridi her iki temada da kırmızıdır. Bu yüzeylerin
///   üstünde Theme.Text ancak temaların BİRİNDE okunur; diğerinde neredeyse
///   görünmez olur. Ray listesi tam olarak buna düşmüştü: açık temada
///   1,40:1.
///
/// Bu test tema-sabit yüzeyleri VARSAYMAZ, HESAPLAR: her yüzey tokenını iki
/// temada çözer ve gövde metniyle uyumunu ölçer. Böylece yarın yeni bir
/// tema-sabit yüzey eklenirse liste kendiliğinden büyür ve o yüzeyin kendi
/// ön plan tokenı olup olmadığı sorulur.
///
/// Tek tek kontrol renklerini değil, TEMA MİMARİSİNİ sınar.
/// </summary>
public class ThemeConstantSurfaceTests
{
    private const string BodyText = "Theme.Text";

    // WPF fırçaları oluşturuldukları thread'e bağlıdır; renkler test
    // thread'inden okunamaz. Bu yüzden tema bir kez barındırıcı thread'inde
    // düz Color sözlüğüne çevrilir (Color bir struct, thread'e bağlı değil).
    private static readonly IReadOnlyDictionary<string, Color> Light = Snapshot(ThemeTestHost.Light);
    private static readonly IReadOnlyDictionary<string, Color> Dark  = Snapshot(ThemeTestHost.Dark);

    private static IReadOnlyDictionary<string, Color> Snapshot(string themeName) =>
        ThemeTestHost.Run(() =>
        {
            var theme = ThemeTestHost.LoadTheme(themeName);

            return theme.Keys.OfType<string>()
                .Where(k => theme[k] is SolidColorBrush)
                .ToDictionary(k => k, k => ((SolidColorBrush)theme[k]).Color, StringComparer.Ordinal)
                as IReadOnlyDictionary<string, Color>;
        });

    private static Color? Resolve(IReadOnlyDictionary<string, Color> theme, string key) =>
        theme.TryGetValue(key, out var color) ? color : null;

    /// <summary>Uygulamada gerçekten ZEMİN olarak kullanılan tokenlar.</summary>
    private static IEnumerable<string> SurfaceTokens()
    {
        var used = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in UiSourceLocator.XamlFiles(includeResources: true))
            foreach (Match m in Regex.Matches(
                         File.ReadAllText(file, Encoding.UTF8),
                         @"Background""?\s*=\s*""\{(?:Dynamic|Static)Resource (?<token>Theme\.[A-Za-z.]+)\}"""))
                used.Add(m.Groups["token"].Value);

        // Ölçülebilmesi için iki temada da çözülmeli VE OPAK olmalı.
        // Yarı saydam katmanlar (Theme.Scrim gibi) bir metin yüzeyi değildir:
        // altındaki rengi bilmeden kontrast hesaplanamaz ve zaten üzerlerine
        // yazı yazılmaz — karartmanın işi arkayı geri çekmektir.
        return used.Where(t => IsOpaque(Light, t) && IsOpaque(Dark, t))
                   .OrderBy(t => t, StringComparer.Ordinal);
    }

    private static bool IsOpaque(IReadOnlyDictionary<string, Color> theme, string token) =>
        Resolve(theme, token) is { A: 255 };

    /// <summary>
    /// Yüzey gövde metnini iki temada da taşıyabiliyor mu.
    /// </summary>
    private static bool CarriesBodyText(string token)
    {
        var lightRatio = Contrast.Ratio(Resolve(Light, BodyText)!.Value, Resolve(Light, token)!.Value);
        var darkRatio  = Contrast.Ratio(Resolve(Dark,  BodyText)!.Value, Resolve(Dark,  token)!.Value);

        return lightRatio >= Contrast.AA && darkRatio >= Contrast.AA;
    }

    /// <summary>
    /// Tema-sabit yüzeylerin listesi HESAPLANIR, elle tutulmaz. Liste
    /// beklenmedik biçimde büyürse yeni bir yüzey gövde metniyle uyumsuz hâle
    /// gelmiş demektir — o zaman kendi ön plan tokenı gerekir.
    /// </summary>
    public static TheoryData<string> IncompatibleSurfaces()
    {
        var data = new TheoryData<string>();
        foreach (var token in SurfaceTokens().Where(t => !CarriesBodyText(t)))
            data.Add(token);
        return data;
    }

    /// <summary>
    /// Hatanın kökü. Ray zemini gövde metnini TAŞIYAMAZ — bu bir kusur değil,
    /// bilinçli bir tasarım: ray her iki temada da koyu bir yüzeydir.
    /// Kusur, ray listesinin gövde metni tokenını okumasıydı.
    ///
    /// Bu gerçek testle sabitlenmezse, ileride biri "ray da diğerleri gibi
    /// olsun" diye örtük stile geri döner ve hata aynen tekrarlanır.
    /// </summary>
    [Fact]
    public void Ray_zemini_govde_metnini_tasiyamaz()
    {
        Assert.False(CarriesBodyText("Theme.Nav.Background"));

        // Açık tema tarafı: hatanın gerçekleştiği yer
        var ratio = Contrast.Ratio(
            Resolve(Light, BodyText)!.Value,
            Resolve(Light, "Theme.Nav.Background")!.Value);

        Assert.True(ratio < Contrast.AA,
            $"Ray zemini artık gövde metnini taşıyor ({ratio:F2}:1) — tasarım değiştiyse bu test güncellenmeli.");
    }

    /// <summary>
    /// SÖZLEŞME: gövde metnini taşıyamayan her yüzeyin, iki temada da AA geçen
    /// KENDİ ön plan tokenı olmalı. Olmazsa o yüzeye yazı yazan geliştiricinin
    /// tek seçeneği ya ham renk vermek ya da gövde metnini kullanmaktır —
    /// birincisi tema mimarisini deler, ikincisi D7.1 hatasını üretir.
    /// </summary>
    [Theory]
    [MemberData(nameof(IncompatibleSurfaces))]
    public void Tema_sabit_yuzeyin_kendi_on_plan_tokeni_var(string surface)
    {
        var candidates = ForegroundCandidates(surface).ToList();

        var uygun = candidates.Where(fg =>
            Contrast.Ratio(Resolve(Light, fg)!.Value, Resolve(Light, surface)!.Value) >= Contrast.AA &&
            Contrast.Ratio(Resolve(Dark,  fg)!.Value, Resolve(Dark,  surface)!.Value) >= Contrast.AA)
            .ToList();

        Assert.True(uygun.Count > 0,
            $"{surface} gövde metnini taşıyamıyor ve iki temada da AA geçen eşlenik bir ön plan " +
            $"tokenı yok. Denenenler: {string.Join(", ", candidates)}");
    }

    /// <summary>
    /// Yüzeyin eşlenik ön plan adayları. Projede iki adlandırma var:
    ///
    ///   Theme.Nav.Background   → aynı ailedeki Theme.Nav.* ön planları
    ///   Theme.Secondary        → Theme.OnSecondary  (On&lt;X&gt; kuralı)
    ///
    /// Arama bilerek DAR: "ailede bir yerlerde uygun bir renk var" demek
    /// yeterli değil, o yüzeyin eşi olarak TASARLANMIŞ bir token aranıyor.
    /// </summary>
    private static IEnumerable<string> ForegroundCandidates(string surface)
    {
        var parts = surface.Split('.');

        if (parts.Length >= 3)
        {
            var family = string.Join('.', parts[..^1]) + ".";

            return Light.Keys
                .Where(k => k.StartsWith(family, StringComparison.Ordinal))
                .Where(k => !k.Contains("Background", StringComparison.Ordinal));
        }

        var on = $"Theme.On{parts[^1]}";
        return Light.ContainsKey(on) ? [on] : [];
    }

    /// <summary>
    /// Tema-sabit yüzeylerin sayısı sessizce artmamalı. Artış kendiliğinden
    /// hata değildir ama BİLİNÇLİ olmalı: yeni yüzey kendi ön plan tokenıyla
    /// birlikte gelmeli.
    /// </summary>
    [Fact]
    public void Tema_sabit_yuzey_listesi_bilinen_kumeyle_sinirli()
    {
        string[] bilinen =
        [
            // Navigasyon rayı — her iki temada koyu lacivert (D7.1'in konusu)
            "Theme.Nav.Background",

            // Baskı önizleme — kâğıdı taklit eder, temayla dönmez
            "Theme.PrintPreview.HeaderBackground",

            // Vurgu yüzeyi — her iki temada da doygun renk
            "Theme.Secondary",
        ];

        var bulunan = SurfaceTokens().Where(t => !CarriesBodyText(t)).ToArray();

        Assert.Equal(bilinen.OrderBy(t => t, StringComparer.Ordinal), bulunan);
    }
}
