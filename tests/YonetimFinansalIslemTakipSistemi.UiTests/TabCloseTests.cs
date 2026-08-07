using System.Text;
using System.Text.RegularExpressions;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Shell;
using static YonetimFinansalIslemTakipSistemi.UiTests.ShellTestDoubles;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Sekme kapatma (Faz D7).
///
/// NEDEN VAR: Ekranlar pencereyken başlık çubuğundaki X ile kapanıyordu.
/// Kabuğa geçişte bu karşılık konmadığı için 13 ekranın sekmesi AÇILIP
/// KAPANMIYORDU — kullanıcı biriken sekmelerden kurtulamıyordu. Derleme ve
/// mevcut testler bunu göremezdi; eksik olan bir davranış değil, bir
/// ARAYÜZ öğesiydi.
///
/// Kapatma üç yoldan sunulur (düğme, orta tık, Ctrl+W) ama karar tek yerde:
/// ShellViewModel.CloseTab. Bu testler hem kapının çalıştığını hem de
/// arayüzün o kapıya bağlı olduğunu sabitler.
/// </summary>
public class TabCloseTests
{
    // Sahte nesneler ve ekran fabrikası ShellTestDoubles'ta — aynı kopyalar
    // dört test dosyasında duruyordu (Faz E9).

    private static ShellViewModel Vm(params ScreenDefinition[] screens) => Shell(screens);

    // ── Kapatma kapısı ───────────────────────────────────────────────────

    [Fact]
    public void Kapatilabilir_sekme_kapanir() => ThemeTestHost.Run(() =>
    {
        var vm = Vm(Screen(ScreenKey.Reports));
        var tab = vm.OpenScreen(ScreenKey.Reports)!;

        Assert.True(vm.CloseTab(tab));
        Assert.Empty(vm.Tabs);
        Assert.Null(vm.ActiveTab);
    });

    /// <summary>
    /// Nakit İşlemler kapatılamaz — finans kullanıcısının ana çalışma alanı.
    /// Kapatma düğmesi de görünmemeli (bkz. XAML sözleşmesi testi).
    /// </summary>
    [Fact]
    public void Kapatilamaz_sekme_kapanmaz() => ThemeTestHost.Run(() =>
    {
        var vm = Vm(Screen(ScreenKey.CashTransactions, canClose: false));
        var tab = vm.OpenScreen(ScreenKey.CashTransactions)!;

        Assert.False(vm.CloseTab(tab));
        Assert.Single(vm.Tabs);
    });

    /// <summary>
    /// Kaydedilmemiş değişiklik kapatmayı reddedebilir; sekme açık kalır.
    /// </summary>
    [Fact]
    public void Ekran_reddederse_sekme_kapanmaz() => ThemeTestHost.Run(() =>
    {
        var blocking = new BlockingScreen { AllowClose = false };
        var vm  = Vm(Screen(ScreenKey.Reports, factory: _ => blocking));
        var tab = vm.OpenScreen(ScreenKey.Reports)!;

        Assert.False(vm.CloseTab(tab));
        Assert.Single(vm.Tabs);

        blocking.AllowClose = true;
        Assert.True(vm.CloseTab(tab));
        Assert.Empty(vm.Tabs);
    });

    /// <summary>
    /// Aktif sekme kapanınca odak komşuya geçer — kabuk boş içerikle kalmaz.
    /// </summary>
    [Fact]
    public void Aktif_sekme_kapaninca_odak_komsuya_gecer() => ThemeTestHost.Run(() =>
    {
        var vm = Vm(Screen(ScreenKey.Reports), Screen(ScreenKey.Analysis), Screen(ScreenKey.AuditLog));

        vm.OpenScreen(ScreenKey.Reports);
        var middle = vm.OpenScreen(ScreenKey.Analysis)!;
        vm.OpenScreen(ScreenKey.AuditLog);

        vm.ActiveTab = middle;
        Assert.True(vm.CloseTab(middle));

        Assert.Equal(2, vm.Tabs.Count);
        Assert.NotNull(vm.ActiveTab);
        Assert.DoesNotContain(vm.Tabs, t => ReferenceEquals(t, middle));
    });

    /// <summary>
    /// Kapatılan ekran tekrar açılabilmeli — kapatma kaydı silmiyor,
    /// yalnızca sekmeyi kapatıyor.
    /// </summary>
    [Fact]
    public void Kapatilan_ekran_tekrar_acilabilir() => ThemeTestHost.Run(() =>
    {
        var vm  = Vm(Screen(ScreenKey.Reports));
        var tab = vm.OpenScreen(ScreenKey.Reports)!;

        vm.CloseTab(tab);
        var reopened = vm.OpenScreen(ScreenKey.Reports);

        Assert.NotNull(reopened);
        Assert.Single(vm.Tabs);
        Assert.NotSame(tab, reopened);
    });

    /// <summary>
    /// Ctrl+W'nin bağlandığı komut aktif sekmeyi kapatır.
    /// </summary>
    [Fact]
    public void CloseTabCommand_aktif_sekmeyi_kapatir() => ThemeTestHost.Run(() =>
    {
        var vm = Vm(Screen(ScreenKey.Reports), Screen(ScreenKey.Analysis));

        vm.OpenScreen(ScreenKey.Reports);
        var second = vm.OpenScreen(ScreenKey.Analysis)!;

        Assert.Same(second, vm.ActiveTab);
        vm.CloseTabCommand.Execute(null);

        Assert.Single(vm.Tabs);
        Assert.DoesNotContain(vm.Tabs, t => ReferenceEquals(t, second));
    });

    /// <summary>
    /// Kapatılamaz tek sekme varken Ctrl+W kabuğu boşaltmamalı.
    /// </summary>
    [Fact]
    public void CloseTabCommand_kapatilamaz_sekmeyi_birakmaz() => ThemeTestHost.Run(() =>
    {
        var vm = Vm(Screen(ScreenKey.CashTransactions, canClose: false));
        vm.OpenScreen(ScreenKey.CashTransactions);

        vm.CloseTabCommand.Execute(null);

        Assert.Single(vm.Tabs);
    });

    // ── Arayüz sözleşmesi ────────────────────────────────────────────────

    private static string ShellMarkup =>
        Regex.Replace(
            File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
                "Views", "Shell", "ShellWindow.xaml"), Encoding.UTF8),
            @"<!--.*?-->", string.Empty, RegexOptions.Singleline);

    private static string ShellCode =>
        File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
            "Views", "Shell", "ShellWindow.xaml.cs"), Encoding.UTF8);

    /// <summary>
    /// Yorumsuz kaynak. Buradaki yorumlar kapatma kapısının NEDEN kabukta
    /// olmadığını anlatırken "CanClose" ve "RequestClose" kelimelerini
    /// geçiriyor; tarama onları çalışan kod sanmamalı.
    /// </summary>
    private static string ShellCodeWithoutComments =>
        Regex.Replace(Regex.Replace(ShellCode, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline),
                      @"//[^\r\n]*", string.Empty);

    /// <summary>Sekme başlığı şablonundaki kapatma düğmesinin işaretlemesi.</summary>
    private static string CloseButtonMarkup =>
        Regex.Match(ShellMarkup, @"<Button\b[^>]*CloseTabButton_Click[\s\S]*?</Button>").Value;

    /// <summary>
    /// Sekme başlığında kapatma düğmesi olmalı. Asıl kayıp buydu: kapı
    /// çalışıyordu ama kullanıcının basacağı bir şey yoktu.
    /// </summary>
    [Fact]
    public void Sekme_basliginda_kapatma_dugmesi_var()
    {
        Assert.Contains("CloseTabButton_Click", ShellMarkup, StringComparison.Ordinal);
        Assert.Contains("TabCloseButton", ShellMarkup, StringComparison.Ordinal);
        Assert.NotEmpty(CloseButtonMarkup);
    }

    /// <summary>
    /// Düğmenin görünürlüğünü SADECE CanClose belirlemeli.
    ///
    /// Bu testi negatif kontrol doğurdu: düğmeye sabit
    /// <c>Visibility="Collapsed"</c> eklediğimde "düğme var mı" testi hâlâ
    /// geçiyordu. Var olması yetmiyor; görünür olması gerekiyor.
    /// </summary>
    [Fact]
    public void Kapatma_dugmesinin_gorunurlugu_yalnizca_CanClose_ile_belirleniyor()
    {
        var button = CloseButtonMarkup;
        Assert.NotEmpty(button);

        // Düğme etiketinin KENDİSİNDE sabit bir Visibility olmamalı
        var openingTag = Regex.Match(button, @"^<Button\b[^>]*>").Value;
        Assert.DoesNotContain("Visibility", openingTag, StringComparison.Ordinal);

        // Stil içindeki tek Visibility setter'ı CanClose=False tetikleyicisinde
        var setters = Regex.Matches(button, @"Property=""Visibility"" Value=""(?<v>\w+)""")
                           .Select(m => m.Groups["v"].Value)
                           .ToList();

        Assert.Equal(["Collapsed"], setters);
    }

    /// <summary>
    /// Kapatılamaz sekmede düğme gizlenmeli; basılınca hiçbir şey olmayan
    /// bir düğme göstermek yanıltıcı olurdu.
    /// </summary>
    [Fact]
    public void Kapatilamaz_sekmede_dugme_gizleniyor()
    {
        Assert.Matches(
            @"DataTrigger Binding=""\{Binding CanClose\}"" Value=""False""[\s\S]{0,200}?Visibility"" Value=""Collapsed""",
            ShellMarkup);
    }

    /// <summary>
    /// Ctrl+Q tercih edilen kısayol; Ctrl+W geriye dönük uyumluluk için
    /// KORUNUR. İkisi de aynı komuta bağlı olmalı — biri farklı bir yola
    /// saparsa kapatma davranışı ikiye bölünür.
    /// </summary>
    [Theory]
    [InlineData("Q")]
    [InlineData("W")]
    public void Kapatma_kisayolu_tanimli(string key)
    {
        Assert.Matches(
            $@"<KeyBinding Key=""{key}"" Modifiers=""Ctrl"" Command=""\{{Binding CloseTabCommand\}}""",
            ShellMarkup);
    }

    /// <summary>
    /// Kapatma için tanımlı TÜM kısayollar tek komuta gitmeli.
    /// </summary>
    [Fact]
    public void Kapatma_kisayollarinin_hepsi_ayni_komuta_bagli()
    {
        var bound = Regex.Matches(ShellMarkup,
                @"<KeyBinding Key=""(?<key>\w+)""[^>]*Command=""\{Binding CloseTabCommand\}""")
            .Select(m => m.Groups["key"].Value)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["Q", "W"], bound);
    }

    [Fact]
    public void Orta_tikla_kapatma_tanimli()
    {
        Assert.Contains("ScreenTabs_PreviewMouseDown", ShellMarkup, StringComparison.Ordinal);
        Assert.Contains("MouseButton.Middle", ShellCode, StringComparison.Ordinal);
    }

    /// <summary>
    /// Üç kapatma yolu da TEK kapıdan geçmeli. Biri CanClose/RequestClose'u
    /// atlarsa kaydedilmemiş değişiklik sessizce kaybolurdu.
    /// </summary>
    [Fact]
    public void Uc_kapatma_yolu_da_ayni_kapidan_gecer()
    {
        var code  = ShellCodeWithoutComments;
        var calls = Regex.Matches(code, @"_vm\.CloseTab\(").Count;

        Assert.True(calls >= 2, $"Kabukta {calls} CloseTab çağrısı var; düğme ve orta tık beklenir.");

        // Kabuk kapatma kararını kendisi vermemeli
        Assert.DoesNotContain("CanClose", code, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestClose", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Tabs.Remove", code, StringComparison.Ordinal);
    }

    /// <summary>
    /// Kapatma düğmesi stili genel TabItem şablonuna KONMAMALI: oradaki bir
    /// düğme uygulamadaki her TabControl'de belirirdi.
    /// </summary>
    [Fact]
    public void Kapatma_dugmesi_genel_TabItem_sablonunda_degil()
    {
        var controls = File.ReadAllText(
            Path.Combine(UiSourceLocator.UiProjectDirectory, "Resources", "Controls.xaml"), Encoding.UTF8);

        // Stil TANIMI burada olabilir (yeniden kullanılabilir olsun diye)
        Assert.Contains(@"x:Key=""TabCloseButton""", controls, StringComparison.Ordinal);

        // ama TabItem şablonunun İÇİNDE kullanılmamalı
        var tabItemTemplate = Regex.Match(controls,
            @"<Style TargetType=""\{x:Type TabItem\}"">[\s\S]*?</Style>").Value;

        Assert.NotEmpty(tabItemTemplate);
        Assert.DoesNotContain("TabCloseButton", tabItemTemplate, StringComparison.Ordinal);
    }

    /// <summary>
    /// Ray vurgusu aktif sekmeyi takip etmeli: kapanan ekran rayda seçili
    /// kalırsa kullanıcıya açık olmayan bir ekranı açıkmış gibi gösterir.
    /// </summary>
    [Fact]
    public void Ray_vurgusu_aktif_sekmeyi_takip_ediyor()
    {
        Assert.Contains("SyncNavigationSelection", ShellCode, StringComparison.Ordinal);
        Assert.Contains("nameof(ShellViewModel.ActiveTab)", ShellCode, StringComparison.Ordinal);
    }
}
