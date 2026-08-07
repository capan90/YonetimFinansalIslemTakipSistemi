using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.Views.Shell;
using static YonetimFinansalIslemTakipSistemi.UiTests.ShellTestDoubles;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Kabuk GERÇEKTEN çiziliyor mu.
///
/// YAKALADIĞI HATA SINIFI: yalnızca ŞABLON MATERYALLEŞİRKEN ortaya çıkan
/// hatalar. Mevcut kabuk testleri ShellWindow'u kuruyordu ama hiç
/// göstermiyor/ölçmüyordu; sekme başlığı şablonu ve sağ tık menüsü hiç
/// üretilmediği için oradaki bir kusur teste yakalanmıyordu.
///
/// Gerçek olay (Faz E, manuel smoke): ItemContainerStyle içindeki
/// Setter.Value ContextMenu'sünün olay bağlantıları, XAML derleyicisinde
/// başka bir x:Name'li öğeyle AYNI connectionId'ye katlandı. Üretilen kod
/// o numarada hedefi Border'a çeviriyordu, sekme şeridi ölçülürken hedef
/// olarak Button geldi ve uygulama login sonrası hiç açılmadı:
///
///     XamlParseException: connectionId ayarlama işlemi özel durum döndürdü
///     └─ InvalidCastException: Button → Border
///
/// Derleme temizdi, 574 UI testi yeşildi. Eksik olan tek şey ölçüm adımıydı.
/// </summary>
public class ShellRenderTests
{
    /// <summary>
    /// Loaded/şablon işleri dispatcher kuyruğunda daha düşük öncelikte bekler;
    /// test gövdesi Normal öncelikte çalıştığı için kuyruk kendiliğinden
    /// boşalmaz.
    /// </summary>
    private static void Flush() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);

    private static ScreenDefinition Tab(ScreenKey key, bool canClose = true) =>
        new(key, key.ToString(), [],
            CreateView: _ => new UserControl(),
            CanClose: canClose);

    private static ShellWindow BuildShell(params ScreenDefinition[] screens)
    {
        var userContext = new FakeUserContext();
        return new ShellWindow(new FakeServices(userContext), screens);
    }

    /// <summary>
    /// Kabuğu gerçekten göster ve ölç. Sekme şeridinin ölçülmesi başlık
    /// şablonunu (kapatma düğmesi + sağ tık menüsü) materyalleştirir —
    /// connectionId hatası tam burada patlıyordu.
    /// </summary>
    [Fact]
    public void Kabuk_sekmeleriyle_birlikte_cizilebiliyor() => ThemeTestHost.Run(() =>
    {
        var shell = BuildShell(
            Tab(ScreenKey.CashTransactions, canClose: false),
            Tab(ScreenKey.Reports),
            Tab(ScreenKey.Analysis));

        var vm = (UI.ViewModels.Shell.ShellViewModel)shell.DataContext;

        vm.OpenScreen(ScreenKey.CashTransactions);
        vm.OpenScreen(ScreenKey.Reports);
        vm.OpenScreen(ScreenKey.Analysis);

        // Show + UpdateLayout: şablonların üretildiği an burasıdır.
        shell.Show();
        shell.UpdateLayout();
        Flush();

        var tabs = FindDescendant<TabControl>(shell);
        Assert.NotNull(tabs);
        Assert.Equal(3, tabs!.Items.Count);

        // Başlık şablonu gerçekten üretildi mi — kapatma düğmesi görünür ağaçta
        var closeButtons = Descendants(tabs).OfType<Button>().ToList();
        Assert.NotEmpty(closeButtons);

        shell.Close();
    });

    /// <summary>
    /// Sekme sağ tık menüsü şablonla birlikte üretilebilmeli. Menü, olay
    /// bağlantısı taşıyan tek yerdi ve hatanın kaynağı oydu.
    /// </summary>
    [Fact]
    public void Sekme_sag_tik_menusu_uretilebiliyor() => ThemeTestHost.Run(() =>
    {
        var shell = BuildShell(Tab(ScreenKey.Reports));
        var vm    = (UI.ViewModels.Shell.ShellViewModel)shell.DataContext;

        vm.OpenScreen(ScreenKey.Reports);

        shell.Show();
        shell.UpdateLayout();
        Flush();

        var menus = Descendants(shell)
            .OfType<FrameworkElement>()
            .Select(e => e.ContextMenu)
            .Where(m => m is not null)
            .ToList();

        Assert.NotEmpty(menus);

        // Menü öğeleri gerçekten kuruldu mu
        var items = menus[0]!.Items.OfType<MenuItem>().Select(i => i.Header?.ToString()).ToList();
        Assert.Contains("Kapat", items);
        Assert.Contains("Tümünü Kapat", items);

        shell.Close();
    });

    /// <summary>
    /// Palet katmanı da kabuk ağacında kurulmalı; kapalıyken görünmez olmalı.
    /// </summary>
    [Fact]
    public void Palet_katmani_kurulu_ve_baslangicta_gizli() => ThemeTestHost.Run(() =>
    {
        var shell = BuildShell(Tab(ScreenKey.Reports));

        shell.Show();
        shell.UpdateLayout();
        Flush();

        var overlay = Descendants(shell)
            .OfType<Border>()
            .FirstOrDefault(b => b.Name == "PaletteOverlay");

        Assert.NotNull(overlay);
        Assert.Equal(Visibility.Collapsed, overlay!.Visibility);

        shell.Close();
    });

    // ── Ağaç yardımcıları ────────────────────────────────────────────────

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        => Descendants(root).OfType<T>().FirstOrDefault();

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);

        for (var i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            yield return child;

            foreach (var nested in Descendants(child))
                yield return nested;
        }
    }
}
