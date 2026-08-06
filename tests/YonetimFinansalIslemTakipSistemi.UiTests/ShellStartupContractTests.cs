using System.Text;
using System.Text.RegularExpressions;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Kabuk iskeleti projeye eklendi ama KULLANICININ AKIŞI DEĞİŞMEDİ.
///
/// Faz D kademeli ilerliyor: önce zemin ve iskelet, sonra pilot ekran, sonra
/// geri kalanlar. Bu testler ara adımda startup'ın kazara ShellWindow'a
/// bağlanmadığını sabitler — kabuk henüz boş ve içine taşınmış ekran yok,
/// bağlansaydı uygulama kullanılamaz hâle gelirdi.
///
/// Pilot dönüşüm onaylandığında bu testler bilinçli olarak güncellenecek.
/// </summary>
public class ShellStartupContractTests
{
    private static string AppStartupSource()
    {
        var path = Path.Combine(UiSourceLocator.UiProjectDirectory, "App.xaml.cs");
        return File.ReadAllText(path, Encoding.UTF8);
    }

    [Fact]
    public void Startup_hala_MainWindow_ve_CargoDashboardWindow_aciyor()
    {
        var source = AppStartupSource();

        Assert.Contains("new MainWindow(", source, StringComparison.Ordinal);
        Assert.Contains("new CargoDashboardWindow(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Startup_henuz_ShellWindow_acmiyor()
    {
        var source = AppStartupSource();

        Assert.DoesNotContain("new ShellWindow(", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Yetkiye göre başlangıç ekranı seçimi duruyor. Kabuk bu kararı henüz
    /// devralmadı; devralınca burası bilinçli olarak değişecek.
    /// </summary>
    [Fact]
    public void ResolveStartupMode_sozlesmesi_duruyor()
    {
        var source = AppStartupSource();

        Assert.Contains("ResolveStartupMode", source, StringComparison.Ordinal);

        foreach (var mode in new[] { "\"finance\"", "\"cargo\"", "\"none\"" })
            Assert.Contains(mode, source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Oturum döngüsünün sözleşmesi: kabuk ShowDialog ile açılır ve
    /// IsLogoutRequested ile döngüye sinyal verir. ShellWindow bu sözleşmeyi
    /// aynen uygular — devralma anında App tarafında yeni bir kavram gerekmez.
    /// </summary>
    [Fact]
    public void ShellWindow_mevcut_logout_sozlesmesini_uygular()
    {
        var path   = Path.Combine(UiSourceLocator.UiProjectDirectory, "Views", "Shell", "ShellWindow.xaml.cs");
        var source = File.ReadAllText(path, Encoding.UTF8);

        Assert.Contains("public bool IsLogoutRequested", source, StringComparison.Ordinal);
        Assert.Contains("IsLogoutRequested = true;", source, StringComparison.Ordinal);
        Assert.Contains("Close();", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Mevcut iki kabuk yerinde duruyor ve dosyaları silinmedi.
    /// </summary>
    [Theory]
    [InlineData("MainWindow.xaml")]
    [InlineData("MainWindow.xaml.cs")]
    [InlineData("Views/Cargo/CargoDashboardWindow.xaml")]
    [InlineData("Views/Cargo/CargoDashboardWindow.xaml.cs")]
    public void Mevcut_kabuklar_yerinde(string relativePath)
    {
        var path = Path.Combine(
            UiSourceLocator.UiProjectDirectory,
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.True(File.Exists(path), $"{relativePath} bulunamadı.");
    }

    /// <summary>
    /// Kargo yetkili kullanıcının kişisel Harf Duyarlılığı erişimi kaybolmamalı.
    /// Kabuğa taşınana kadar tek yolu Kargo Dashboard'daki Yardım menüsüdür
    /// (bkz. docs/03-Modules/UserSettings.md).
    /// </summary>
    [Fact]
    public void Kargo_kullanicisinin_harf_duyarliligi_erisimi_duruyor()
    {
        var path   = Path.Combine(UiSourceLocator.UiProjectDirectory,
                                  "Views", "Cargo", "CargoDashboardWindow.xaml");
        var source = File.ReadAllText(path, Encoding.UTF8);

        Assert.Contains("Harf Duyarlılığı", source, StringComparison.Ordinal);
        Assert.Contains("OpenTextCaseSettings_Click", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Kabuk ekranları için tanımlanan yetkiler MEVCUT permission modelinden
    /// gelmeli; kabuk kendine yeni yetki türü uydurmamalı.
    /// </summary>
    [Fact]
    public void Kabuk_yeni_permission_turu_tanimlamiyor()
    {
        var enumPath = Path.Combine(
            UiSourceLocator.UiProjectDirectory, "..",
            "YonetimFinansalIslemTakipSistemi.Domain", "Enums", "PermissionType.cs");

        var declared = Regex.Matches(File.ReadAllText(enumPath, Encoding.UTF8),
                                     @"^\s*(?<name>Can\w+)\s*=", RegexOptions.Multiline)
                            .Select(m => m.Groups["name"].Value)
                            .ToHashSet(StringComparer.Ordinal);

        var registryPath = Path.Combine(
            UiSourceLocator.UiProjectDirectory, "Common", "Shell", "ScreenRegistry.cs");

        var used = Regex.Matches(File.ReadAllText(registryPath, Encoding.UTF8),
                                 @"PermissionType\.(?<name>\w+)")
                        .Select(m => m.Groups["name"].Value)
                        .Distinct(StringComparer.Ordinal)
                        .ToList();

        Assert.NotEmpty(used);

        var unknown = used.Where(u => !declared.Contains(u)).ToList();
        Assert.True(unknown.Count == 0,
            "Kabuk mevcut permission modelinde olmayan yetki kullanıyor: " + string.Join(", ", unknown));
    }
}
