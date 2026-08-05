namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Her pencere XAML'ini gerçek kaynak zinciriyle ayrıştırır.
///
/// NEDEN: WPF'te StaticResource/DynamicResource hataları DERLEME ZAMANINDA
/// yakalanmaz. "Build yeşil" bir pencerenin açılırken XamlParseException
/// atmayacağı anlamına gelmez. Bu test tüm pencereleri hem açık hem koyu temada
/// ayrıştırarak o boşluğu kapatır: çözülemeyen kaynak, hatalı şablon veya
/// bozuk stil zinciri burada patlar.
///
/// Ayrıştırma kod arkasından arındırılmış kaynak üzerinden yapılır; ayrıntı
/// ve gerekçe için bkz. <see cref="XamlSanitizer"/>.
/// </summary>
public class XamlLoadTests
{
    public static TheoryData<string> WindowFiles()
    {
        var data = new TheoryData<string>();
        foreach (var file in UiSourceLocator.XamlFiles(includeResources: false))
            data.Add(UiSourceLocator.Relative(file));
        return data;
    }

    [Fact]
    public void Taranan_pencere_sayisi_beklenen_araliktadir()
    {
        // Dosya sayısı düşerse (ör. glob bozulursa) test sessizce "her şey yolunda"
        // demesin diye alt sınır konur.
        var count = UiSourceLocator.XamlFiles(includeResources: false).Count;
        Assert.True(count >= 35, $"Yalnızca {count} pencere XAML'i bulundu — tarama eksik olabilir.");
    }

    [Theory]
    [MemberData(nameof(WindowFiles))]
    public void Pencere_acik_temada_ayristirilir(string relativePath) => AssertParses(relativePath, ThemeTestHost.Light);

    [Theory]
    [MemberData(nameof(WindowFiles))]
    public void Pencere_koyu_temada_ayristirilir(string relativePath) => AssertParses(relativePath, ThemeTestHost.Dark);

    private static void AssertParses(string relativePath, string themeName)
    {
        var fullPath = Path.Combine(
            UiSourceLocator.UiProjectDirectory,
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        ThemeTestHost.ApplyTheme(themeName);

        ThemeTestHost.Run(() =>
        {
            try
            {
                var root = XamlSanitizer.Parse(fullPath);
                Assert.NotNull(root);
            }
            catch (Exception ex)
            {
                throw new Xunit.Sdk.XunitException(
                    $"{relativePath} ({themeName}) ayrıştırılamadı:{Environment.NewLine}{ex.Message}");
            }
        });
    }
}
