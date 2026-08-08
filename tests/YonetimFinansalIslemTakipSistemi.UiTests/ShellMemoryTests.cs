using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using YonetimFinansalIslemTakipSistemi.UI.Common;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using static YonetimFinansalIslemTakipSistemi.UiTests.ShellTestDoubles;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Kabuk yaşam döngüsü: bellek ve abonelik ölçümü (Faz F4).
///
/// ÖLÇMEDEN İYİLEŞTİRME YOK. Buradaki testler tahmin doğrulamaz, sayı
/// üretir: sekme kapandıktan sonra ekran GC'ye uygun mu, statik olaya
/// yapılan abonelik sekme geçişinde ne oluyor.
///
/// Ölçümün konusu WPF'in şu davranışı: kabuğun TabControl'ü tek bir
/// ContentPresenter kullanır ve sekme değişince giden ekranı görsel ağaçtan
/// SÖKER — yani <c>Unloaded</c> her geçişte tetiklenir (bkz.
/// TabLifecycleTests). Kurucuda abone olup Unloaded'da çıkan bir ekran, ilk
/// sekme geçişinden sonra bir daha abone OLMAZ.
/// </summary>
public class ShellMemoryTests
{
    private static void Flush() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);

    private static void Collect()
    {
        for (var i = 0; i < 3; i++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();
        }
    }

    private sealed class FakeTab(string title, FrameworkElement view)
    {
        public string           Title { get; } = title;
        public FrameworkElement View  { get; } = view;
    }

    /// <summary>Kabuğun sekme kurulumunun aynısı (bkz. ShellWindow.xaml).</summary>
    private static (Window Window, TabControl Tabs) ShellLikeTabs(IEnumerable<FrameworkElement> screens)
    {
        var tabs = new TabControl
        {
            ItemsSource = screens.Select((s, i) => new FakeTab($"Sekme {i}", s)).ToList(),

            ItemTemplate = (DataTemplate)XamlReader.Parse(
                """
                <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                    <TextBlock Text="{Binding Title}"/>
                </DataTemplate>
                """),

            ContentTemplate = (DataTemplate)XamlReader.Parse(
                """
                <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                    <ContentPresenter Content="{Binding View}"/>
                </DataTemplate>
                """),
        };

        var window = new Window { Content = tabs, Width = 500, Height = 400 };
        window.Show();
        window.UpdateLayout();
        Flush();

        return (window, tabs);
    }

    // ── Statik olay aboneliği ────────────────────────────────────────────

    /// <summary>
    /// Grafik ekranlarının kullandığı desen: statik olaya KURUCUDA abone ol,
    /// <c>Unloaded</c>'da çık.
    /// </summary>
    private sealed class SubscribeInConstructor : UserControl
    {
        public int RepaintCount;

        public SubscribeInConstructor()
        {
            ChartPalette.ThemeChanged += Repaint;
            Unloaded += (_, _) => ChartPalette.ThemeChanged -= Repaint;
        }

        private void Repaint() => RepaintCount++;
    }

    /// <summary>
    /// ÖLÇÜM — hatanın kanıtı. Pencere modelinde <c>Unloaded</c> ömürde bir
    /// kez (kapanışta) tetikleniyordu, bu yüzden desen doğruydu. Kabukta her
    /// sekme geçişinde tetikleniyor: ekran bir kez başka sekmeye geçildiğinde
    /// aboneliğini kaybediyor ve GERİ DÖNÜNCE yeniden abone olmuyor.
    ///
    /// Görünen sonuç: tema değiştirilince grafikler eski renklerinde donuyor —
    /// ama yalnızca kullanıcı daha önce sekme değiştirdiyse. Bu yüzden
    /// "bazen oluyor" diye bildirilir.
    /// </summary>
    [Fact]
    public void Kurucuda_abone_olan_ekran_sekme_donusunde_aboneligini_kaybediyor() => ThemeTestHost.Run(() =>
    {
        var screen = new SubscribeInConstructor();
        var other  = new UserControl();

        var (window, tabs) = ShellLikeTabs([screen, other]);

        ChartPalette.NotifyThemeChanged();
        Flush();
        var ilkGosterimde = screen.RepaintCount;

        // Başka sekmeye geç → Unloaded → abonelikten çıkar
        tabs.SelectedIndex = 1;
        window.UpdateLayout();
        Flush();

        // Geri dön → kurucu YENİDEN ÇALIŞMAZ, abonelik geri gelmez
        tabs.SelectedIndex = 0;
        window.UpdateLayout();
        Flush();

        ChartPalette.NotifyThemeChanged();
        Flush();
        var donusSonrasi = screen.RepaintCount;

        window.Close();

        Assert.True(ilkGosterimde > 0, "İlk gösterimde abonelik çalışmıyor — ölçüm kurulumu hatalı.");
        Assert.Equal(ilkGosterimde, donusSonrasi);   // ← kusur: dönüşte artmıyor
    });

    /// <summary>
    /// DOĞRU DESEN: <c>ScreenData.BindThemeRepaint</c> aboneliği Loaded'da
    /// kurar, Unloaded'da söker. Sekmeye dönüldüğünde abonelik geri gelir.
    /// </summary>
    [Fact]
    public void Bagli_ekran_sekme_donusunde_yeniden_abone_oluyor() => ThemeTestHost.Run(() =>
    {
        var repaints = 0;
        var screen   = new UserControl();
        ScreenData.BindThemeRepaint(screen, () => repaints++);

        var (window, tabs) = ShellLikeTabs([screen, new UserControl()]);

        var ilkGosterimde = repaints;   // Loaded bir kez boyar

        tabs.SelectedIndex = 1;
        window.UpdateLayout();
        Flush();

        // Ekran gizliyken tema değişirse boyama tetiklenmemeli
        ChartPalette.NotifyThemeChanged();
        Flush();
        Assert.Equal(ilkGosterimde, repaints);

        tabs.SelectedIndex = 0;
        window.UpdateLayout();
        Flush();

        // Geri dönünce: bir kez telafi boyaması (gizliyken tema değişmiş olabilir)
        Assert.True(repaints > ilkGosterimde, "Sekmeye dönüldüğünde yeniden boyanmadı.");

        var donusSonrasi = repaints;
        ChartPalette.NotifyThemeChanged();
        Flush();

        window.Close();

        Assert.True(repaints > donusSonrasi, "Dönüşten sonra abonelik geri gelmedi.");
    });

    /// <summary>
    /// Ekran GÖRSEL AĞAÇTAN çıktığında statik olayda abonelik kalmamalı —
    /// statik olay uygulama ömrü boyunca yaşar ve tuttuğu ekran hiç
    /// toplanmaz.
    /// </summary>
    [Fact]
    public void Kapanan_ekran_statik_olayda_abonelik_birakmiyor() => ThemeTestHost.Run(() =>
    {
        var repaints = 0;
        var screen   = new UserControl();
        ScreenData.BindThemeRepaint(screen, () => repaints++);

        var (window, _) = ShellLikeTabs([screen]);
        window.Close();
        Flush();

        var kapanista = repaints;
        ChartPalette.NotifyThemeChanged();
        Flush();

        Assert.Equal(kapanista, repaints);
    });

    // ── Bellek ───────────────────────────────────────────────────────────

    /// <summary>
    /// Aynı anahtar iki kez açılamaz (tekillik); 15 sekme için 15 ayrı ekran.
    /// </summary>
    private static readonly ScreenKey[] FifteenKeys =
    [
        ScreenKey.Reports, ScreenKey.Analysis, ScreenKey.AuditLog, ScreenKey.SystemLogs,
        ScreenKey.Users, ScreenKey.Permissions, ScreenKey.ExchangeRates, ScreenKey.CargoDashboard,
        ScreenKey.IncomingCargo, ScreenKey.OutgoingCargo, ScreenKey.CompanyDirectory,
        ScreenKey.CargoCompanies, ScreenKey.WhatsAppContacts, ScreenKey.MailContacts,
        ScreenKey.SystemHealth
    ];

    /// <summary>
    /// Aç/kapat döngüsü AYRI BİR ÇERÇEVEDE çalışır ve yalnızca zayıf
    /// referansları döndürür.
    ///
    /// Bu şart: Debug derlemesinde yerel değişkenler metot sonuna kadar kök
    /// sayılır. Döngüyü test gövdesinde çalıştırırsak açılan sekmeleri testin
    /// KENDİ yerelleri canlı tutar ve ölçüm "sızıntı var" der — ölçtüğümüz şey
    /// kabuk değil, test olur.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<WeakReference> OpenAndCloseAll(out int opened, out int remaining)
    {
        var vm   = Shell(FifteenKeys.Select(k => Screen(k)).ToArray());
        var refs = new List<WeakReference>();

        foreach (var key in FifteenKeys)
        {
            var tab = vm.OpenScreen(key);
            refs.Add(new WeakReference(tab!.View));
        }

        opened = vm.Tabs.Count;

        foreach (var tab in vm.Tabs.ToList())
            vm.CloseTab(tab);

        remaining = vm.Tabs.Count;
        return refs;
    }

    /// <summary>
    /// ÖLÇÜM: 15 sekme aç/kapat, sonra ekranların toplanabildiğini doğrula.
    ///
    /// Kabuk sekmeyi kapatırken bağladığı her şeyi söker (ShellViewModel.Detach).
    /// Sökmezse kapatılan ekran kabuk yaşadığı sürece bellekte kalır ve
    /// kullanıcı "uygulama şişiyor" der.
    /// </summary>
    [Fact]
    public void On_bes_sekme_acilip_kapatildiktan_sonra_ekranlar_toplanabiliyor() => ThemeTestHost.Run(() =>
    {
        var refs = OpenAndCloseAll(out var opened, out var remaining);

        Assert.Equal(15, opened);
        Assert.Equal(0, remaining);

        Collect();

        var alive = refs.Count(r => r.IsAlive);
        Assert.True(alive == 0, $"Kapatılan 15 ekranın {alive} tanesi hâlâ bellekte tutuluyor.");
    });

    /// <summary>
    /// Sekme kapatıldığında kabuk gezgin referansını da bırakmalı: ekran
    /// kabuğa, kabuk ekrana bakarsa ikisi birlikte hayatta kalır.
    /// </summary>
    [Fact]
    public void Kapanan_sekme_gezgin_referansini_birakiyor() => ThemeTestHost.Run(() =>
    {
        var screen = new NavigationAwareScreen();
        var vm     = Shell(Screen(ScreenKey.Reports, factory: _ => screen));

        var tab = vm.OpenScreen(ScreenKey.Reports)!;
        Assert.NotNull(screen.Navigator);

        vm.CloseTab(tab);

        Assert.Null(screen.Navigator);
    });

    private sealed class NavigationAwareScreen : UserControl, IShellNavigationAware
    {
        public IShellNavigator? Navigator { get; set; }
    }

    // ── Kaynak sözleşmesi ────────────────────────────────────────────────

    /// <summary>
    /// BEKÇİ: hiçbir ekran statik olaya KURUCUDAN abone olmamalı. Kurucu bir
    /// kez çalışır, Unloaded her sekme geçişinde — ikisi eşleşmez.
    /// </summary>
    [Fact]
    public void Hicbir_ekran_statik_olaya_kurucudan_abone_olmuyor()
    {
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(
                     UiSourceLocator.UiProjectDirectory, "*Screen.xaml.cs", SearchOption.AllDirectories))
        {
            var code = File.ReadAllText(file, Encoding.UTF8);
            code = Regex.Replace(code, @"//[^\r\n]*", string.Empty);

            if (Regex.IsMatch(code, @"ChartPalette\.ThemeChanged\s*\+="))
                violations.Add(Path.GetFileName(file));
        }

        Assert.True(violations.Count == 0,
            "Ekran statik olaya doğrudan abone oluyor (ScreenData.BindThemeRepaint kullanılmalı): "
            + string.Join(", ", violations));
    }
}
