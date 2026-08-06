using System.Text;
using System.Text.RegularExpressions;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Test barındırıcısının yüklediği sözlükler ile App.xaml'inkiler aynı olmalı.
///
/// NEDEN VAR: <see cref="ThemeTestHost"/> uygulamanın kaynak zincirini elle
/// kurar. App.xaml'e yeni bir sözlük eklenip buraya eklenmezse tüm tema
/// testleri EKSİK bir zincir üzerinde çalışır ve yanlış yere "yeşil" der.
///
/// Faz C'de tam olarak bu oldu: ChartPalette.xaml App.xaml'e eklendi, test
/// barındırıcısına eklenmedi. XAML parse testi yakaladı ama hata mesajı
/// ("StaticResourceExtension değer sağlayamadı") nedeni göstermiyordu.
/// Bu test nedeni doğrudan söyler.
/// </summary>
public class ResourceDictionaryParityTests
{
    [Fact]
    public void Test_barindiricisi_App_xaml_ile_ayni_sozlukleri_yukler()
    {
        var appXaml = Path.Combine(UiSourceLocator.UiProjectDirectory, "App.xaml");
        var markup  = File.ReadAllText(appXaml, Encoding.UTF8);

        // <ResourceDictionary Source="Resources/X.xaml"/>
        var declared = Regex.Matches(markup, @"<ResourceDictionary\s+Source=""(?<src>[^""]+)""")
                            .Select(m => m.Groups["src"].Value)
                            .ToList();

        Assert.NotEmpty(declared);

        // Tema sözlüğü çalışma zamanında takas edilir; barındırıcı onu ayrı yükler.
        var expected = declared
            .Where(s => !s.Contains("Themes/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Equal(expected, ThemeTestHost.AppDictionaries);
    }

    [Fact]
    public void App_xaml_tema_sozlugunu_en_sona_yukler()
    {
        var appXaml = Path.Combine(UiSourceLocator.UiProjectDirectory, "App.xaml");
        var markup  = File.ReadAllText(appXaml, Encoding.UTF8);

        var declared = Regex.Matches(markup, @"<ResourceDictionary\s+Source=""(?<src>[^""]+)""")
                            .Select(m => m.Groups["src"].Value)
                            .ToList();

        // Tema en sonda olmalı: sözlükte son kayıt önceliklidir ve tema
        // token'ları diğer sözlüklerdeki varsayılanları ezebilmelidir.
        Assert.Contains("Themes/", declared[^1], StringComparison.OrdinalIgnoreCase);
    }
}
