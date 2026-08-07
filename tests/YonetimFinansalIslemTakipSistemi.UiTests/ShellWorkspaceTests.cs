using System.Text;
using System.Text.RegularExpressions;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using static YonetimFinansalIslemTakipSistemi.UiTests.ShellTestDoubles;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Kabuk çalışma alanı: toplu sekme kapatma, sekmeler arası gezinme ve
/// sekme şeridi (Faz E2 / E4 / E5).
///
/// ORTAK İLKE: kabuk arayüzü ne kadar yol sunarsa sunsun, karar tek yerde
/// kalır — ShellViewModel. Kapatılamaz sekme ve kaydedilmemiş değişiklik
/// kontrolü menüde, kısayolda ve düğmede TEKRARLANMAZ.
/// </summary>
public class ShellWorkspaceTests
{
    private static ShellViewModelFixture ThreeTabs() => new();

    /// <summary>Üç sekmeli kabuk; testler sırayı ve aktif sekmeyi kullanır.</summary>
    private sealed class ShellViewModelFixture
    {
        public readonly UI.ViewModels.Shell.ShellViewModel Vm;
        public readonly UI.ViewModels.Shell.ShellTab       First;
        public readonly UI.ViewModels.Shell.ShellTab       Middle;
        public readonly UI.ViewModels.Shell.ShellTab       Last;

        public ShellViewModelFixture()
        {
            Vm = Shell(
                Screen(ScreenKey.Reports),
                Screen(ScreenKey.Analysis),
                Screen(ScreenKey.AuditLog));

            First  = Vm.OpenScreen(ScreenKey.Reports)!;
            Middle = Vm.OpenScreen(ScreenKey.Analysis)!;
            Last   = Vm.OpenScreen(ScreenKey.AuditLog)!;
        }
    }

    // ── Toplu kapatma (E4) ───────────────────────────────────────────────

    [Fact]
    public void Digerlerini_kapat_yalnizca_verileni_birakir() => ThemeTestHost.Run(() =>
    {
        var f = ThreeTabs();

        Assert.Equal(2, f.Vm.CloseOtherTabs(f.Middle));

        Assert.Single(f.Vm.Tabs);
        Assert.Same(f.Middle, f.Vm.Tabs[0]);
    });

    [Fact]
    public void Sagdakileri_kapat_solu_ve_kendisini_birakir() => ThemeTestHost.Run(() =>
    {
        var f = ThreeTabs();

        Assert.Equal(1, f.Vm.CloseTabsToTheRight(f.Middle));

        Assert.Equal(2, f.Vm.Tabs.Count);
        Assert.Same(f.First,  f.Vm.Tabs[0]);
        Assert.Same(f.Middle, f.Vm.Tabs[1]);
    });

    [Fact]
    public void Sagdakileri_kapat_son_sekmede_hicbir_sey_yapmaz() => ThemeTestHost.Run(() =>
    {
        var f = ThreeTabs();

        Assert.Equal(0, f.Vm.CloseTabsToTheRight(f.Last));
        Assert.Equal(3, f.Vm.Tabs.Count);
    });

    /// <summary>
    /// "Tümünü Kapat" KULLANICININ isteğidir; kapatılamaz sekme açık kalır.
    /// Çıkıştaki CloseAllTabs bundan ayrıdır ve CanClose'u bilerek yok sayar —
    /// ikisi karışırsa ya çıkışta sekme kalır ya da kullanıcı ana çalışma
    /// alanını kaybeder.
    /// </summary>
    [Fact]
    public void Tumunu_kapat_kapatilamaz_sekmeyi_birakir() => ThemeTestHost.Run(() =>
    {
        var vm = Shell(
            Screen(ScreenKey.CashTransactions, canClose: false),
            Screen(ScreenKey.Reports),
            Screen(ScreenKey.Analysis));

        var sabit = vm.OpenScreen(ScreenKey.CashTransactions)!;
        vm.OpenScreen(ScreenKey.Reports);
        vm.OpenScreen(ScreenKey.Analysis);

        Assert.Equal(2, vm.CloseClosableTabs());

        Assert.Single(vm.Tabs);
        Assert.Same(sabit, vm.Tabs[0]);
    });

    [Fact]
    public void Digerlerini_kapat_kapatilamaz_sekmeyi_birakir() => ThemeTestHost.Run(() =>
    {
        var vm = Shell(
            Screen(ScreenKey.CashTransactions, canClose: false),
            Screen(ScreenKey.Reports),
            Screen(ScreenKey.Analysis));

        vm.OpenScreen(ScreenKey.CashTransactions);
        vm.OpenScreen(ScreenKey.Reports);
        var kalan = vm.OpenScreen(ScreenKey.Analysis)!;

        vm.CloseOtherTabs(kalan);

        Assert.Equal(2, vm.Tabs.Count);
        Assert.Contains(vm.Tabs, t => t.Key == ScreenKey.CashTransactions);
        Assert.Contains(vm.Tabs, t => ReferenceEquals(t, kalan));
    });

    /// <summary>
    /// Bir ekranın itirazı diğerlerini durdurmamalı. Aksi hâlde kullanıcı
    /// "diğerlerini kapat" der, hiçbir şey olmaz ve nedenini göremez —
    /// sessiz başarısızlık.
    /// </summary>
    [Fact]
    public void Reddeden_ekran_digerlerinin_kapanmasini_engellemez() => ThemeTestHost.Run(() =>
    {
        var blocking = new BlockingScreen { AllowClose = false };

        var vm = Shell(
            Screen(ScreenKey.Reports,  factory: _ => blocking),
            Screen(ScreenKey.Analysis),
            Screen(ScreenKey.AuditLog));

        vm.OpenScreen(ScreenKey.Reports);
        vm.OpenScreen(ScreenKey.Analysis);
        var kalan = vm.OpenScreen(ScreenKey.AuditLog)!;

        Assert.Equal(1, vm.CloseOtherTabs(kalan));

        Assert.Equal(2, vm.Tabs.Count);
        Assert.Contains(vm.Tabs, t => ReferenceEquals(t.View, blocking));
        Assert.True(blocking.CloseAttempts > 0);
    });

    /// <summary>Aktif sekme toplu kapatmada gittiyse odak boşta kalmamalı.</summary>
    [Fact]
    public void Toplu_kapatmadan_sonra_aktif_sekme_gecerli_kalir() => ThemeTestHost.Run(() =>
    {
        var f = ThreeTabs();
        f.Vm.ActiveTab = f.Last;

        f.Vm.CloseOtherTabs(f.First);

        Assert.Same(f.First, f.Vm.ActiveTab);
        Assert.Contains(f.Vm.Tabs, t => ReferenceEquals(t, f.Vm.ActiveTab));
    });

    // ── Gezinme (E5) ─────────────────────────────────────────────────────

    [Fact]
    public void Sonraki_sekme_sonda_basa_doner() => ThemeTestHost.Run(() =>
    {
        var f = ThreeTabs();
        f.Vm.ActiveTab = f.Last;

        f.Vm.ActivateNextTab();

        Assert.Same(f.First, f.Vm.ActiveTab);
    });

    [Fact]
    public void Onceki_sekme_basta_sona_doner() => ThemeTestHost.Run(() =>
    {
        var f = ThreeTabs();
        f.Vm.ActiveTab = f.First;

        f.Vm.ActivatePreviousTab();

        Assert.Same(f.Last, f.Vm.ActiveTab);
    });

    [Fact]
    public void Sekme_sirasi_serit_sirasidir() => ThemeTestHost.Run(() =>
    {
        var f = ThreeTabs();

        f.Vm.ActivateTabAt(1);
        Assert.Same(f.First, f.Vm.ActiveTab);

        f.Vm.ActivateTabAt(3);
        Assert.Same(f.Last, f.Vm.ActiveTab);
    });

    /// <summary>Olmayan sekmeye basınca hiçbir şey olmamalı — hata da değil.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(9)]
    public void Aralik_disi_sekme_istegi_yok_sayilir(int position) => ThemeTestHost.Run(() =>
    {
        var f = ThreeTabs();
        f.Vm.ActiveTab = f.Middle;

        f.Vm.ActivateTabAt(position);

        Assert.Same(f.Middle, f.Vm.ActiveTab);
    });

    [Fact]
    public void Tek_sekmede_gezinme_ayni_sekmede_kalir() => ThemeTestHost.Run(() =>
    {
        var vm  = Shell(Screen(ScreenKey.Reports));
        var tab = vm.OpenScreen(ScreenKey.Reports)!;

        vm.ActivateNextTab();
        vm.ActivatePreviousTab();

        Assert.Same(tab, vm.ActiveTab);
    });

    [Fact]
    public void Bos_kabukta_gezinme_cokmez() => ThemeTestHost.Run(() =>
    {
        var vm = Shell(Screen(ScreenKey.Reports));

        vm.ActivateNextTab();
        vm.ActivatePreviousTab();
        vm.ActivateTabAt(1);

        Assert.Null(vm.ActiveTab);
    });

    // ── Arayüz sözleşmesi ────────────────────────────────────────────────

    private static string ShellMarkup =>
        Regex.Replace(
            File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
                "Views", "Shell", "ShellWindow.xaml"), Encoding.UTF8),
            @"<!--.*?-->", string.Empty, RegexOptions.Singleline);

    private static string Controls =>
        File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
            "Resources", "Controls.xaml"), Encoding.UTF8);

    private static string ShellTabControlStyle =>
        Regex.Match(Controls, @"<Style x:Key=""ShellTabControl""[\s\S]*?</Style>\s*\r?\n\s*</ResourceDictionary>").Value;

    /// <summary>
    /// Yenile düğmesi E1'in zorunlu tamamlayıcısı: otomatik tazeleme
    /// kalktıysa açık tazeleme görünür olmalı, yoksa kullanıcı eski veriye
    /// mahkûm kalır ve bunu fark etmez.
    /// </summary>
    [Fact]
    public void Sekme_seridinde_yenile_dugmesi_var()
    {
        var style = ShellTabControlStyle;

        Assert.NotEmpty(style);
        Assert.Contains("PART_Refresh", style, StringComparison.Ordinal);
        Assert.Contains("common:AppCommands.RefreshList", style, StringComparison.Ordinal);
    }

    /// <summary>
    /// Şerit TEK SATIR kalmalı: WPF'in hazır TabPanel'i sekmeler sığmayınca
    /// ikinci satır açar ve içerik alanını aşağı iter. 18 ekran açılabilen bir
    /// kabukta bu çalışma alanını yer.
    /// </summary>
    [Fact]
    public void Sekme_seridi_satirlara_bolunmez_kaydirilir()
    {
        var style = ShellTabControlStyle;

        var scroller = Regex.Match(style, @"<ScrollViewer[\s\S]*?</ScrollViewer>").Value;

        Assert.NotEmpty(scroller);
        Assert.Contains(@"HorizontalScrollBarVisibility=""Auto""", scroller, StringComparison.Ordinal);
        Assert.Contains(@"VerticalScrollBarVisibility=""Disabled""", scroller, StringComparison.Ordinal);
        Assert.Contains("IsItemsHost=\"True\"", scroller, StringComparison.Ordinal);
    }

    /// <summary>
    /// Şerit şablonu KABUĞA ÖZGÜ kalmalı. Örtük TabControl stili uygulamadaki
    /// HER sekmeye uygulanıyor (içe aktarma sihirbazları dahil); yenile düğmesi
    /// ve kaydırma oraya konsaydı hepsinde belirirdi.
    /// </summary>
    [Fact]
    public void Serit_sablonu_genel_TabControl_stiline_konmadi()
    {
        var ortuk = Regex.Match(Controls,
            @"<Style TargetType=""\{x:Type TabControl\}"">[\s\S]*?</Style>").Value;

        Assert.NotEmpty(ortuk);
        Assert.DoesNotContain("PART_Refresh",  ortuk, StringComparison.Ordinal);
        Assert.DoesNotContain("ScrollViewer",  ortuk, StringComparison.Ordinal);

        Assert.Contains(@"Style=""{StaticResource ShellTabControl}""", ShellMarkup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Kabuk stili örtük stilden TÜREMELİ: renk/kenarlık kararları tek yerde
    /// kalsın, şerit yalnızca yerleşimi değiştirsin. Kopyalanırsa tema
    /// değişikliği birinde uygulanır, diğerinde unutulur.
    /// </summary>
    [Fact]
    public void Kabuk_serit_stili_ortuk_stilden_turuyor()
    {
        Assert.Matches(
            @"<Style x:Key=""ShellTabControl""[\s\S]{0,200}?BasedOn=""\{StaticResource \{x:Type TabControl\}\}""",
            Controls);
    }

    /// <summary>
    /// Şerit AYRI bir yüzey olmamalı — Faz D kararı. Kendi zeminini boyarsa
    /// kabuk ile ekran arasında iç içe kart görüntüsü doğar.
    /// </summary>
    [Fact]
    public void Serit_ayri_bir_zemin_boyamıyor()
    {
        var strip = Regex.Match(ShellTabControlStyle,
            @"<Border DockPanel\.Dock=""Top""[\s\S]*?>").Value;

        Assert.NotEmpty(strip);
        Assert.DoesNotContain("Background=", strip, StringComparison.Ordinal);
    }

    [Fact]
    public void Sekme_sag_tik_menusu_dort_secenek_sunuyor()
    {
        var menu = Regex.Match(ShellMarkup, @"<ContextMenu>[\s\S]*?</ContextMenu>").Value;

        Assert.NotEmpty(menu);
        Assert.Contains("TabMenuClose_Click",       menu, StringComparison.Ordinal);
        Assert.Contains("TabMenuCloseOthers_Click", menu, StringComparison.Ordinal);
        Assert.Contains("TabMenuCloseRight_Click",  menu, StringComparison.Ordinal);
        Assert.Contains("TabMenuCloseAll_Click",    menu, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Tab", "Ctrl",       "NextTabCommand")]
    [InlineData("Tab", "Ctrl\\+Shift", "PreviousTabCommand")]
    public void Sekme_gezinme_kisayollari_tanimli(string key, string modifiers, string command)
    {
        Assert.Matches(
            $@"<KeyBinding Key=""{key}"" Modifiers=""{modifiers}""\s+Command=""\{{Binding {command}\}}""",
            ShellMarkup);
    }

    [Fact]
    public void Ctrl_rakam_kisayollari_dokuz_sekmeyi_kapsiyor()
    {
        var bound = Regex.Matches(ShellMarkup,
                @"<KeyBinding Key=""D(?<n>[1-9])"" Modifiers=""Ctrl"" Command=""\{Binding ActivateTabCommand\}"" CommandParameter=""(?<p>\d)""")
            .Select(m => (n: m.Groups["n"].Value, p: m.Groups["p"].Value))
            .ToList();

        Assert.Equal(9, bound.Count);

        // Tuş ile sıra eşleşmeli; Ctrl+3 üçüncü sekmeyi açmalı
        Assert.All(bound, b => Assert.Equal(b.n, b.p));
    }

    /// <summary>
    /// Toplu kapatma kararı kabukta TEKRARLANMAMALI — menü yalnızca çağırır.
    /// </summary>
    [Fact]
    public void Toplu_kapatma_karari_kabukta_tekrarlanmiyor()
    {
        var code = File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
            "Views", "Shell", "ShellWindow.xaml.cs"), Encoding.UTF8);

        var codeWithoutComments =
            Regex.Replace(Regex.Replace(code, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline),
                          @"//[^\r\n]*", string.Empty);

        Assert.DoesNotContain("CanClose",     codeWithoutComments, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestClose", codeWithoutComments, StringComparison.Ordinal);
        Assert.DoesNotContain("Tabs.Remove",  codeWithoutComments, StringComparison.Ordinal);
    }
}
