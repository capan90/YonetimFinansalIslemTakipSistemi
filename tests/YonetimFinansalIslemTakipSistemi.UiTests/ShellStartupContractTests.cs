using System.Text;
using System.Text.RegularExpressions;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Başlangıç akışının sözleşmesi.
///
/// Faz D5'te YALNIZCA finans dalı kabuğa geçti: finans yetkili kullanıcı artık
/// ShellWindow görüyor. Kargo ve "none" dalları değişmedi, MainWindow silinmedi.
/// Bu testler geçişin kapsamını sabitler — bir sonraki adımda kargo dalı da
/// taşınırsa burası BİLİNÇLİ olarak güncellenmeli, sessizce kaymamalı.
/// </summary>
public class ShellStartupContractTests
{
    private static string AppStartupSource()
    {
        var path = Path.Combine(UiSourceLocator.UiProjectDirectory, "App.xaml.cs");
        return File.ReadAllText(path, Encoding.UTF8);
    }

    /// <summary>
    /// Yetkili kullanıcı hangi modülden olursa olsun ShellWindow görür;
    /// başlangıç akışı eski iki kabuğu ARTIK oluşturmaz.
    ///
    /// Dosyalar yerinde duruyor (geri dönüş için) ama startup içinde değil.
    /// </summary>
    [Fact]
    public void Startup_yalnizca_ShellWindow_aciyor()
    {
        var source = AppStartupSource();

        Assert.Contains("new Views.Shell.ShellWindow(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new MainWindow(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new CargoDashboardWindow(", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Yetkisiz kullanıcı davranışı değişmedi: pencere açılmaz, uyarı gösterilir
    /// ve oturum kapatılır.
    /// </summary>
    [Fact]
    public void None_dali_uyari_gosterip_cikis_yapiyor()
    {
        var source = AppStartupSource();

        Assert.Contains("startupMode == \"none\"", source, StringComparison.Ordinal);
        Assert.Contains("Bu kullanıcı için tanımlı bir başlangıç ekranı bulunamadı", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Yetkiye göre başlangıç kararı duruyor. Kabuk bu kararı DEĞİŞTİRMEDİ;
    /// yalnızca "finance" dalının açtığı pencere değişti.
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
    /// Kargo yetkili kullanıcının kişisel Harf Duyarlılığı erişimi kaybolmamalı
    /// (bkz. docs/03-Modules/UserSettings.md).
    ///
    /// Kargo kullanıcısı artık kabuğu görüyor; erişim kabuğun araç bölümünden.
    /// Kargo panosu ekranındaki yardım menüsü de duruyor — ince barındırıcı
    /// pencerede barındığında tek yol o.
    /// </summary>
    [Fact]
    public void Kargo_kullanicisinin_harf_duyarliligi_erisimi_duruyor()
    {
        var shell = File.ReadAllText(
            Path.Combine(UiSourceLocator.UiProjectDirectory, "Views", "Shell", "ShellWindow.xaml"), Encoding.UTF8);

        Assert.Contains("Harf Duyarlılığı", shell, StringComparison.Ordinal);
        Assert.Contains("OpenTextCaseSettings_Click", shell, StringComparison.Ordinal);

        var dashboard = File.ReadAllText(
            Path.Combine(UiSourceLocator.UiProjectDirectory, "Views", "Cargo", "CargoDashboardScreen.xaml"), Encoding.UTF8);

        Assert.Contains("Harf Duyarlılığı", dashboard, StringComparison.Ordinal);
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
