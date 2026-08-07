using System.Text;
using System.Text.RegularExpressions;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Shell;
using static YonetimFinansalIslemTakipSistemi.UiTests.ShellTestDoubles;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Komut paleti (Faz E6).
///
/// Paletin tek işi ARAMAK; açma kararı kabuğundur. Bu ayrım testlerin de
/// omurgası: filtreleme saf mantık olarak sınanır, yetki kapısının paletle
/// delinmediği ise kabuk üzerinden.
/// </summary>
public class CommandPaletteTests
{
    private static ScreenDefinition Item(ScreenKey key, string title, string group = "") =>
        new(key, title, [], CreateView: _ => new System.Windows.Controls.UserControl(), NavGroup: group);

    private static CommandPaletteViewModel Palette() => new(
    [
        Item(ScreenKey.CashTransactions, "Nakit İşlemler",  "Finans"),
        Item(ScreenKey.Reports,          "Raporlar",        "Finans"),
        Item(ScreenKey.Analysis,         "Finans Analiz",   "Finans"),
        Item(ScreenKey.CargoDashboard,   "Kargo Dashboard", "Kargo Takip"),
        Item(ScreenKey.IncomingCargo,    "Gelen Kargolar",  "Kargo Takip"),
        Item(ScreenKey.Users,            "Kullanıcılar",    "Yönetim"),
    ]);

    // ── Arama ────────────────────────────────────────────────────────────

    [Fact]
    public void Bos_sorgu_tum_ekranlari_gosterir()
    {
        var palette = Palette();

        Assert.Equal(6, palette.Results.Count);
    }

    [Fact]
    public void Baslikta_arar()
    {
        var palette = Palette();
        palette.Query = "rapor";

        Assert.Single(palette.Results);
        Assert.Equal("Raporlar", palette.Results[0].Title);
    }

    /// <summary>
    /// Grup adı da aranır: kullanıcı ekranın adını değil ait olduğu bölümü
    /// hatırlıyor olabilir.
    /// </summary>
    [Fact]
    public void Grup_adiyla_da_arar()
    {
        var palette = Palette();
        palette.Query = "kargo takip";

        Assert.Equal(2, palette.Results.Count);
        Assert.All(palette.Results, r => Assert.Equal("Kargo Takip", r.NavGroup));
    }

    /// <summary>
    /// tr-TR duyarsızlık. OrdinalIgnoreCase "İŞLEM" ile "işlem"i FARKLI sayar
    /// (I↔ı eşleşmez) ve kullanıcı aradığını bulamazdı.
    /// </summary>
    [Theory]
    [InlineData("İŞLEM")]
    [InlineData("işlem")]
    [InlineData("İşlem")]
    public void Turkce_harf_duyarsiz_arar(string query)
    {
        var palette = Palette();
        palette.Query = query;

        Assert.Contains(palette.Results, r => r.Title == "Nakit İşlemler");
    }

    /// <summary>
    /// Duyarsızlık "her şey birbirine uysun" demek DEĞİL: Türkçede ı ve i
    /// ayrı harflerdir, "ışlem" bir kelime değildir ve "İşlemler"i getirmemeli.
    /// InvariantIgnoreCase kullanılsaydı getirirdi — arama gürültülenirdi.
    /// </summary>
    [Fact]
    public void Noktasiz_i_noktali_i_ile_eslesmiyor()
    {
        var palette = Palette();
        palette.Query = "ışlem";

        Assert.Empty(palette.Results);
    }

    /// <summary>
    /// Aranan kelimeyle BAŞLAYAN ekran önce gelmeli: "Kargo" yazan kullanıcı
    /// büyük ihtimalle "Kargo Dashboard" arıyor, "Gelen Kargolar" değil.
    /// </summary>
    [Fact]
    public void Baslayan_eslesme_once_gelir()
    {
        var palette = Palette();
        palette.Query = "kargo";

        Assert.Equal(2, palette.Results.Count);
        Assert.Equal("Kargo Dashboard", palette.Results[0].Title);
    }

    [Fact]
    public void Eslesme_yoksa_liste_bos_ve_secim_yok()
    {
        var palette = Palette();
        palette.Query = "zzzz";

        Assert.Empty(palette.Results);
        Assert.Null(palette.Selected);
    }

    [Fact]
    public void Bosluklar_kirpilir()
    {
        var palette = Palette();
        palette.Query = "   rapor   ";

        Assert.Single(palette.Results);
    }

    // ── Gezinme ──────────────────────────────────────────────────────────

    [Fact]
    public void Ilk_sonuc_bastan_secili()
    {
        var palette = Palette();

        Assert.Equal(0, palette.SelectedIndex);
        Assert.NotNull(palette.Selected);
    }

    [Fact]
    public void Gezinme_listenin_iki_ucunda_donuyor()
    {
        var palette = Palette();

        palette.MovePrevious();
        Assert.Equal(palette.Results.Count - 1, palette.SelectedIndex);

        palette.MoveNext();
        Assert.Equal(0, palette.SelectedIndex);
    }

    /// <summary>
    /// Sorgu değişince vurgu başa dönmeli; yoksa kullanıcı yazmaya devam
    /// ederken seçim listede kayar ve Enter beklenmedik ekranı açar.
    /// </summary>
    [Fact]
    public void Sorgu_degisince_secim_basa_doner()
    {
        var palette = Palette();

        palette.MoveNext();
        palette.MoveNext();
        Assert.Equal(2, palette.SelectedIndex);

        palette.Query = "a";
        Assert.Equal(0, palette.SelectedIndex);
    }

    [Fact]
    public void Bos_listede_gezinme_cokmez()
    {
        var palette = Palette();
        palette.Query = "zzzz";

        palette.MoveNext();
        palette.MovePrevious();

        Assert.Null(palette.Selected);
    }

    // ── Kabukla ilişkisi ─────────────────────────────────────────────────

    [Fact]
    public void Palet_secimi_sekme_aciyor() => ThemeTestHost.Run(() =>
    {
        var vm = Shell(Screen(ScreenKey.Reports), Screen(ScreenKey.Analysis));

        vm.OpenPalette();
        vm.Palette.Query = ScreenKey.Analysis.ToString();

        Assert.True(vm.AcceptPalette());

        Assert.Single(vm.Tabs);
        Assert.Equal(ScreenKey.Analysis, vm.Tabs[0].Key);
        Assert.False(vm.IsPaletteOpen);
    });

    /// <summary>
    /// PALET YENİ KAPI AÇMAZ. Yetkisiz ekran zaten listeye girmez; girse bile
    /// açma isteği OpenScreen'in yetki kapısından geçer.
    /// </summary>
    [Fact]
    public void Palet_yetkisiz_ekrani_listelemiyor() => ThemeTestHost.Run(() =>
    {
        var vm = Shell(
            [
                Screen(ScreenKey.Reports, PermissionType.CanViewReports),
                Screen(ScreenKey.Users,   PermissionType.CanManageUsers),
            ],
            PermissionType.CanViewReports);

        vm.OpenPalette();

        Assert.Single(vm.Palette.Results);
        Assert.Equal(ScreenKey.Reports, vm.Palette.Results[0].Key);
    });

    [Fact]
    public void Palet_her_acilista_sorguyu_sifirliyor() => ThemeTestHost.Run(() =>
    {
        var vm = Shell(Screen(ScreenKey.Reports), Screen(ScreenKey.Analysis));

        vm.OpenPalette();
        vm.Palette.Query = "rapor";
        vm.ClosePalette();

        vm.OpenPalette();

        Assert.Equal(string.Empty, vm.Palette.Query);
        Assert.Equal(2, vm.Palette.Results.Count);
    });

    [Fact]
    public void Bos_secimle_kabul_hicbir_sey_yapmaz() => ThemeTestHost.Run(() =>
    {
        var vm = Shell(Screen(ScreenKey.Reports));

        vm.OpenPalette();
        vm.Palette.Query = "zzzz";

        Assert.False(vm.AcceptPalette());
        Assert.Empty(vm.Tabs);

        // Sonuç yoksa palet AÇIK kalır: kullanıcı aramasını düzeltebilmeli
        Assert.True(vm.IsPaletteOpen);
    });

    // ── Arayüz sözleşmesi ────────────────────────────────────────────────

    private static string ShellMarkup =>
        Regex.Replace(
            File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
                "Views", "Shell", "ShellWindow.xaml"), Encoding.UTF8),
            @"<!--.*?-->", string.Empty, RegexOptions.Singleline);

    [Fact]
    public void Ctrl_K_paleti_aciyor()
    {
        Assert.Matches(
            @"<KeyBinding Key=""K"" Modifiers=""Ctrl"" Command=""\{Binding OpenPaletteCommand\}""",
            ShellMarkup);
    }

    /// <summary>
    /// Palet ayrı bir PENCERE olmamalı: kabuğun içinde bir katman olarak durur,
    /// böylece sahiplik/odak sorunu çıkmaz ve kabuk kapanınca gider.
    /// </summary>
    [Fact]
    public void Palet_kabuk_icinde_bir_katman()
    {
        Assert.Contains("PaletteOverlay", ShellMarkup, StringComparison.Ordinal);
        Assert.Contains("Palette.Results", ShellMarkup, StringComparison.Ordinal);

        var uiDir = UiSourceLocator.UiProjectDirectory;
        Assert.False(Directory.EnumerateFiles(uiDir, "*Palette*Window.xaml", SearchOption.AllDirectories).Any(),
            "Palet ayrı pencere olarak yazılmış.");
    }

    [Fact]
    public void Palet_klavyeyle_surulebiliyor()
    {
        var code = File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
            "Views", "Shell", "ShellWindow.xaml.cs"), Encoding.UTF8);

        foreach (var key in new[] { "Key.Escape", "Key.Enter", "Key.Down", "Key.Up" })
            Assert.Contains(key, code, StringComparison.Ordinal);
    }
}
