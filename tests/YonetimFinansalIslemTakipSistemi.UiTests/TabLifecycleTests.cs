using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using YonetimFinansalIslemTakipSistemi.UI.Common;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Sekme yaşam döngüsü ve ekran veri yüklemesi (Faz E1).
///
/// YAKALADIĞI HATA SINIFI: pencere modelinden kabuğa geçerken SESSİZCE
/// değişen bir varsayım. Ekranlar pencereyken <c>Loaded</c> ömürde bir kez
/// tetikleniyordu; veriyi oradan çekmek doğruydu. Kabukta WPF TabControl TEK
/// bir ContentPresenter kullanır ve seçim değişince giden ekranı görsel
/// ağaçtan söker — <c>Loaded</c> her sekme geçişinde yeniden tetiklenir.
///
/// Derleme de mevcut testler de bunu göremezdi: kod doğruydu, DAVRANIŞ
/// bağlamı değişmişti. Ölçüm olmadan da görünmezdi, çünkü sonuç "yanlış
/// veri" değil "aynı veriyi tekrar tekrar sorgulamak"tı.
/// </summary>
public class TabLifecycleTests
{
    // ── Yardımcılar ──────────────────────────────────────────────────────

    /// <summary>
    /// Loaded/Unloaded olayları dispatcher kuyruğunda Loaded önceliğinde
    /// bekler; test gövdesi Normal öncelikte çalıştığı için kuyruk
    /// kendiliğinden boşalmaz. Daha DÜŞÜK öncelikli (ContextIdle) boş bir
    /// çağrı, üstündeki her şeyin işlenmesini bekler.
    /// </summary>
    private static void Flush() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);

    private sealed class CountingScreen : UserControl
    {
        public int LoadedCount;
        public int UnloadedCount;

        public CountingScreen()
        {
            Loaded   += (_, _) => LoadedCount++;
            Unloaded += (_, _) => UnloadedCount++;
        }
    }

    /// <summary>ShellTab'ın test karşılığı — kabuk XAML'i Title ve View bağlar.</summary>
    private sealed class FakeTab(string title, FrameworkElement view)
    {
        public string           Title { get; } = title;
        public FrameworkElement View  { get; } = view;
    }

    /// <summary>
    /// Kabuğun sekme kurulumunun aynısı: ItemTemplate başlığı, ContentTemplate
    /// içindeki ContentPresenter ekranı taşır (bkz. ShellWindow.xaml).
    /// </summary>
    private static (Window Window, TabControl Tabs) ShellLikeTabs(params FrameworkElement[] screens)
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

        var window = new Window { Content = tabs, Width = 400, Height = 300 };
        window.Show();
        window.UpdateLayout();
        Flush();

        return (window, tabs);
    }

    private static void Switch(Window window, TabControl tabs, int index)
    {
        tabs.SelectedIndex = index;
        window.UpdateLayout();
        Flush();
    }

    // ── WPF gerçeği ──────────────────────────────────────────────────────

    /// <summary>
    /// DÜZELTMENİN DAYANAĞI. Bu davranış değişirse (WPF sürümü, kabuk şablonu)
    /// ScreenData'nın varlık sebebi ortadan kalkar ve bu test önce düşer.
    ///
    /// Sekme geçişi ekranı görsel ağaçtan söküp geri takar; Loaded YENİDEN
    /// tetiklenir. Ekran örneği aynı kalır — kaybolan durum değil, ödenen
    /// bedel tekrar tekrar yüklemedir.
    /// </summary>
    [Fact]
    public void Sekme_gecisi_ekrani_soker_ve_Loaded_yeniden_tetiklenir() => ThemeTestHost.Run(() =>
    {
        var a = new CountingScreen();
        var b = new CountingScreen();

        var (window, tabs) = ShellLikeTabs(a, b);

        Assert.Equal(1, a.LoadedCount);
        Assert.Equal(0, a.UnloadedCount);

        Switch(window, tabs, 1);
        Assert.Equal(1, a.UnloadedCount);
        Assert.Equal(1, b.LoadedCount);

        Switch(window, tabs, 0);
        Assert.Equal(2, a.LoadedCount);   // ← hatanın kaynağı
        Assert.Equal(1, b.UnloadedCount);

        window.Close();
    });

    // ── ScreenData sözleşmesi ────────────────────────────────────────────

    [Fact]
    public void Ekran_sekme_gecislerinde_yalnizca_bir_kez_yuklenir() => ThemeTestHost.Run(() =>
    {
        var loads = 0;

        var a = new UserControl();
        ScreenData.Bind(a, () => { loads++; return Task.CompletedTask; });

        var (window, tabs) = ShellLikeTabs(a, new UserControl());

        Assert.Equal(1, loads);

        Switch(window, tabs, 1);
        Switch(window, tabs, 0);
        Switch(window, tabs, 1);
        Switch(window, tabs, 0);

        Assert.Equal(1, loads);

        window.Close();
    });

    /// <summary>
    /// Hazırlık işi (yetki görünürlüğü, filtre kutuları, kolon düzeni)
    /// yenilemede TEKRARLANMAMALI — kullanıcının o sırada yaptığı seçimi
    /// sıfırlardı.
    /// </summary>
    [Fact]
    public void Hazirlik_yalnizca_ilk_gosterimde_calisir() => ThemeTestHost.Run(() =>
    {
        var loads = 0;
        var inits = 0;

        var screen = new UserControl();
        ScreenData.Bind(screen,
            load:       () => { loads++; return Task.CompletedTask; },
            initialize: () => { inits++; return Task.CompletedTask; });

        var (window, tabs) = ShellLikeTabs(screen, new UserControl());

        Switch(window, tabs, 1);
        Switch(window, tabs, 0);

        AppCommands.RefreshList.Execute(null, screen);
        Flush();

        Assert.Equal(1, inits);
        Assert.Equal(2, loads);   // ilk gösterim + kullanıcının yenilemesi

        window.Close();
    });

    /// <summary>
    /// Otomatik tazeleme kalktığına göre AÇIK yenileme çalışmak zorunda —
    /// yoksa kullanıcı eski veriye mahkûm kalırdı. E1 ile E2 birbirine bağlı.
    /// </summary>
    [Fact]
    public void Yenile_komutu_veriyi_tekrar_yukler() => ThemeTestHost.Run(() =>
    {
        var loads = 0;

        var screen = new UserControl();
        ScreenData.Bind(screen, () => { loads++; return Task.CompletedTask; });

        var (window, _) = ShellLikeTabs(screen);
        Assert.Equal(1, loads);

        AppCommands.RefreshList.Execute(null, screen);
        Flush();
        Assert.Equal(2, loads);

        AppCommands.RefreshList.Execute(null, screen);
        Flush();
        Assert.Equal(3, loads);

        window.Close();
    });

    /// <summary>
    /// Kendi F5 bağlamasını kuran ekranlara ikinci bağlama eklenmemeli;
    /// eklenirse tek tuşla iki sorgu giderdi.
    /// </summary>
    [Fact]
    public void Kendi_yenile_baglamasi_olan_ekrana_ikinci_baglama_eklenmez() => ThemeTestHost.Run(() =>
    {
        var kendi   = 0;
        var ortak   = 0;
        var screen  = new UserControl();

        screen.CommandBindings.Add(new System.Windows.Input.CommandBinding(
            AppCommands.RefreshList, (_, _) => kendi++));

        ScreenData.Bind(screen, () => { ortak++; return Task.CompletedTask; });

        var (window, _) = ShellLikeTabs(screen);

        AppCommands.RefreshList.Execute(null, screen);
        Flush();

        Assert.Equal(1, kendi);
        Assert.Equal(1, ortak);   // yalnızca ilk gösterim; yenilemeyi ekran kendi yaptı

        window.Close();
    });

    /// <summary>
    /// Kapatılıp yeniden açılan ekran YENİ bir örnektir ve yeniden yüklenir —
    /// "bir kez yükle" kapısı örneğe bağlıdır, ekran türüne değil.
    /// </summary>
    [Fact]
    public void Yeniden_acilan_ekran_yeniden_yuklenir() => ThemeTestHost.Run(() =>
    {
        var loads = 0;
        Func<Task> load = () => { loads++; return Task.CompletedTask; };

        var ilk = new UserControl();
        ScreenData.Bind(ilk, load);
        var (w1, _) = ShellLikeTabs(ilk);
        w1.Close();

        var ikinci = new UserControl();
        ScreenData.Bind(ikinci, load);
        var (w2, _) = ShellLikeTabs(ikinci);
        w2.Close();

        Assert.Equal(2, loads);
    });

    // ── Kaynak sözleşmesi ────────────────────────────────────────────────

    /// <summary>
    /// REGRESYON KAPISI: hiçbir kabuk ekranı veriyi kendi <c>Loaded</c>'ından
    /// çekmemeli. Yeni bir ekran eski alışkanlıkla yazılırsa hata sessizce
    /// geri gelir — çalışır görünür, yalnızca yavaştır.
    ///
    /// Bilinçli istisna: CargoShipmentListScreen sekmeye dönüldüğünde
    /// tazeler (operasyon merkezi ayrı sekmede kaydı değiştirebiliyor); o
    /// IsVisibleChanged kullanır, Loaded değil.
    /// </summary>
    [Fact]
    public void Hicbir_ekran_veriyi_Loaded_uzerinden_cekmiyor()
    {
        var ihlaller = new List<string>();

        foreach (var file in Directory.EnumerateFiles(
                     UiSourceLocator.UiProjectDirectory, "*Screen.xaml.cs", SearchOption.AllDirectories))
        {
            var code = File.ReadAllText(file, Encoding.UTF8);

            // "Loaded += ..." içinde yükleme çağrısı
            foreach (Match m in Regex.Matches(code, @"Loaded\s*\+=[^;]*?(LoadAsync|LoadDashboardAsync|LoadTransactionsAsync)"))
                ihlaller.Add($"{Path.GetFileName(file)}: {m.Value.Trim()}");
        }

        foreach (var file in Directory.EnumerateFiles(
                     UiSourceLocator.UiProjectDirectory, "*Screen.xaml", SearchOption.AllDirectories))
        {
            var markup = File.ReadAllText(file, Encoding.UTF8);

            if (Regex.IsMatch(markup, @"\bLoaded\s*="))
                ihlaller.Add($"{Path.GetFileName(file)}: XAML'de Loaded bağlaması");
        }

        Assert.True(ihlaller.Count == 0,
            "Ekranlar veriyi ScreenData üzerinden yüklemeli:\n  " + string.Join("\n  ", ihlaller));
    }

    /// <summary>
    /// Her kabuk ekranı yükleme kapısını kullanmalı. Kullanmayan bir ekran ya
    /// verisini hiç yüklemiyordur ya da kendi yolunu açmıştır — ikisi de
    /// bilerek yapılmışsa bu listeye yazılır.
    /// </summary>
    [Fact]
    public void Tum_kabuk_ekranlari_yukleme_kapisini_kullaniyor()
    {
        // Verisi olmayan / yüklemesini kurucuda tamamlayan ekranlar
        string[] muaf =
        [
            "CargoOperationCenterScreen.xaml.cs",   // parametreyle gelen kayıt üzerinde çalışır
        ];

        var eksik = Directory
            .EnumerateFiles(UiSourceLocator.UiProjectDirectory, "*Screen.xaml.cs", SearchOption.AllDirectories)
            .Where(f => !muaf.Contains(Path.GetFileName(f)))
            .Where(f => !File.ReadAllText(f, Encoding.UTF8).Contains("ScreenData.", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToList();

        Assert.True(eksik.Count == 0,
            "ScreenData kullanmayan ekranlar: " + string.Join(", ", eksik));
    }
}
