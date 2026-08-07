using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Shell;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Shell;

/// <summary>
/// Tek kabuk penceresi — finans pilotu (Faz D5).
///
/// Finans yetkili kullanıcının başlangıç ekranı artık burasıdır; Nakit
/// İşlemler sekmesi açılışta otomatik açılır. Kargo-only kullanıcı hâlâ
/// CargoDashboardWindow'a gider, "none" davranışı değişmedi.
///
/// ÇIKIŞ SÖZLEŞMESİ mevcut kabuklarla BİREBİR aynı:
/// <see cref="IsLogoutRequested"/> + <c>Close()</c>. App.xaml.cs'teki
/// login → kabuk → logout → login döngüsü hiç değişmeden çalışır.
///
/// Bu pencere HİÇBİR EKRANI TANIMAZ: sekme içerikleri
/// <see cref="ScreenRegistry"/> üzerinden üretilir, kısayollar aktif sekmeye
/// yönlendirilir. Ekran taşındıkça buraya kod eklemek gerekmez.
/// </summary>
public partial class ShellWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly IDialogService   _dialogService;
    private readonly ShellViewModel   _vm;

    /// <summary>
    /// Mevcut MainWindow / CargoDashboardWindow ile aynı sözleşme:
    /// pencere kapandığında App bunu okuyup oturum döngüsüne devam eder.
    /// </summary>
    public bool IsLogoutRequested { get; private set; }

    public ShellWindow(IServiceProvider services)
        : this(services, ScreenRegistry.All)
    {
    }

    /// <summary>
    /// Ekran listesi dışarıdan alınabilir — testler kendi tanımlarını geçebilir.
    /// </summary>
    internal ShellWindow(IServiceProvider services, IReadOnlyList<ScreenDefinition> screens)
    {
        InitializeComponent();

        _services      = services;
        _dialogService = services.GetRequiredService<IDialogService>();

        var userContext = services.GetRequiredService<IUserContext>();

        _vm = new ShellViewModel(services, userContext, screens);

        // Onay ViewModel'in çıkış akışının BAŞINDA sorulur; sekmeler ancak
        // kullanıcı onayladıktan sonra kapatılır (bkz. ShellViewModel).
        _vm.ConfirmLogout    = () => Common.SessionLogout.Confirm(_dialogService);
        _vm.LogoutRequested += OnLogoutRequested;

        DataContext = _vm;

        StatusUserText.Text    = userContext.FullName;
        StatusVersionText.Text = $"Sürüm {Assembly.GetExecutingAssembly().GetName().Version}";

        ApplyToolVisibility(userContext);
        OpenDefaultScreen();

        // Pencere X ile kapatılırsa da açık ekranlara söz hakkı verilir;
        // kaydedilmemiş değişiklik sessizce kaybolmasın.
        Closing += OnClosing;

        // Rayın vurgusu aktif sekmeyi takip eder — sekme kapanınca kapalı
        // ekran seçili kalmasın.
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ShellViewModel.ActiveTab))
                SyncNavigationSelection();

            if (e.PropertyName == nameof(ShellViewModel.IsPaletteOpen))
                OnPaletteVisibilityChanged();
        };

        Loaded += async (_, _) =>
        {
            // Ray öğeleri ItemsControl tarafından üretiliyor; kurucu anında
            // henüz görsel ağaçta yoklar.
            SyncNavigationSelection();

            // Açılışta güncelleme kontrolü — uygulama seviyesi bir iş ve artık
            // kabuğun sorumluluğu. Eskiden MainWindow ve Kargo Panosu ayrı ayrı
            // yürütüyordu; kabuk ikisinin de yerini alınca tek yerde kaldı.
            // Ekran yüklendikten sonra, kullanıcıyı bloklamadan çalışır.
            await Services.StartupUpdateChecker.RunOnceAsync(_services, _dialogService);
        };
    }

    /// <summary>
    /// Açılışta hangi sekme açılır.
    ///
    /// Finans kullanıcısında Nakit İşlemler, kargo kullanıcısında rayın ilk
    /// öğesi (Kargo Dashboard). Ayrı bir tablo tutulmuyor: yetkisi olmayan
    /// ekran zaten rayda yok ve OpenScreen null döner. Yetki kontrolü burada
    /// TEKRARLANMAZ — tek kapı ShellViewModel.Resolve'dur.
    /// </summary>
    private void OpenDefaultScreen()
    {
        var opened = _vm.OpenScreen(ScreenKey.CashTransactions);

        if (opened is null && _vm.NavigationItems.Count > 0)
            opened = _vm.OpenScreen(_vm.NavigationItems[0].Key);

        if (opened is not null)
            _vm.SelectedNavigationItem = opened.Definition;
    }

    /// <summary>
    /// Ekran AÇMAYAN araç düğmelerinin yetki kapıları.
    ///
    /// Kapılar MainWindow.RefreshMenuVisibility ve
    /// CargoDashboardScreen.ApplyNavBarVisibility'den birebir alındı; kabuk
    /// yeni bir yetki kuralı uydurmuyor. Ekranların kapıları burada DEĞİL,
    /// ScreenRegistry'de (bkz. ShellViewModel.IsVisible).
    /// </summary>
    private void ApplyToolVisibility(IUserContext userContext)
    {
        static Visibility Show(bool allowed) => allowed ? Visibility.Visible : Visibility.Collapsed;

        var canSettings = userContext.HasPermission(PermissionType.CanAccessSettings);
        var canManage   = userContext.HasPermission(PermissionType.CanManageUsers);
        var canHelp     = userContext.HasPermission(PermissionType.CanAccessHelpMenu);

        ToolMailSettings.Visibility = Show(canSettings);
        ToolAppearance.Visibility   = Show(canSettings);

        // DB testi ve log klasörü MainWindow'da yönetici kapısındaydı
        ToolDbTest.Visibility    = Show(canManage);
        ToolLogFolder.Visibility = Show(canManage || canHelp);

        ToolCheckUpdates.Visibility = Show(canHelp);
        ToolPersonalMail.Visibility = Show(canHelp);

        // Harf Duyarlılığı ve Çıkış her kullanıcıda görünür — kapı yok.
    }

    // ── Ekran açmayan araç eylemleri ──────────────────────────────────────
    //
    // Gövdeleri Common/ToolActions ve Common/UpdateCheckFlow içinde: aynı
    // eylemler MainWindow ve Kargo Panosu'ndan da başlatılabiliyor.

    private void OpenMailSettings_Click(object sender, RoutedEventArgs e)
        => new Settings.MailSettingsWindow(_services, isPersonal: false) { Owner = this }.ShowDialog();

    private void OpenPersonalMailSettings_Click(object sender, RoutedEventArgs e)
        => new Settings.MailSettingsWindow(_services, isPersonal: true) { Owner = this }.ShowDialog();

    private void OpenAppearanceSettings_Click(object sender, RoutedEventArgs e)
        => new Settings.AppearanceSettingsWindow(_services) { Owner = this }.ShowDialog();

    private async void TestDbConnection_Click(object sender, RoutedEventArgs e)
        => await Common.ToolActions.TestDatabaseAsync(_services, _dialogService);

    private void OpenLogDirectory_Click(object sender, RoutedEventArgs e)
        => Common.ToolActions.OpenLogDirectory(_dialogService);

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        => await Common.UpdateCheckFlow.RunAsync(_services, _dialogService);

    // ── Navigasyon ────────────────────────────────────────────────────────

    private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Seçimi kabuk kendisi ayarlıyorsa (aktif sekme değişti) yeniden
        // ekran açmaya çalışma — sonsuz döngü olur.
        if (_syncingNavigation) return;

        if (e.AddedItems.Count == 0) return;
        if (e.AddedItems[0] is not ScreenDefinition screen) return;

        _vm.SelectedNavigationItem = screen;

        // Zaten açık ekran ikinci sekme üretmez; ShellViewModel mevcut sekmeye
        // odaklanır (bkz. OpenScreen). Rayın vurgusu ActiveTab değişince
        // SyncNavigationSelection tarafından güncellenir.
        _vm.OpenScreen(screen.Key);
    }

    private bool _syncingNavigation;

    /// <summary>
    /// Rayın vurgusunu AKTİF SEKMEYE eşitler.
    ///
    /// İki nedenle gerekli:
    ///   • Ray gruplara bölündü ve her grubun kendi ListBox'ı var; ikisinde
    ///     birden seçili öğe kalmamalı.
    ///   • Sekme kapanınca kapalı ekran rayda seçili kalmamalı — kullanıcıya
    ///     açık olmayan bir ekranı açıkmış gibi gösterirdi.
    /// </summary>
    private void SyncNavigationSelection()
    {
        var key = _vm.ActiveTab?.Key;

        _syncingNavigation = true;
        try
        {
            foreach (var list in Descendants(NavigationGroupList).OfType<ListBox>())
                list.SelectedItem = list.Items
                    .OfType<ScreenDefinition>()
                    .FirstOrDefault(s => key is not null && s.Key == key);
        }
        finally
        {
            _syncingNavigation = false;
        }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }

    // ── Sekme kapatma ─────────────────────────────────────────────────────
    //
    // Ekranlar pencereyken X ile kapanıyordu; sekmede karşılığı üç yoldan
    // verilir: başlıktaki düğme, orta tık, Ctrl+W. Üçü de aynı kapıdan
    // geçer — ShellViewModel.CloseTab, CanClose ve RequestClose'u kontrol
    // eder, karar burada TEKRARLANMAZ.

    private void CloseTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ShellTab tab })
            _vm.CloseTab(tab);
    }

    /// <summary>
    /// Orta tıkla sekme kapatma — tarayıcı alışkanlığı.
    ///
    /// Tıklanan noktadaki TabItem görsel ağaçtan bulunur: TabControl'ün
    /// kendisi hangi sekmeye basıldığını olay üzerinden vermez.
    /// </summary>
    private void ScreenTabs_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle) return;
        if (e.OriginalSource is not DependencyObject source) return;

        var item = FindAncestor<TabItem>(source);
        if (item?.DataContext is ShellTab tab)
        {
            _vm.CloseTab(tab);
            e.Handled = true;
        }
    }

    // ── Komut paleti (Faz E6) ─────────────────────────────────────────────
    //
    // Palet ayrı bir pencere DEĞİL, kabuğun üstüne serilen bir katman: sahiplik
    // ve odak sorunları çıkmıyor, kabuk kapanınca kendiliğinden gidiyor.
    //
    // Görünürlük ViewModel'deki IsPaletteOpen'a bağlı; kod arkası yalnızca
    // odağı ve klavyeyi yönetir — hangi ekranın açılacağına ViewModel karar
    // verir (bkz. ShellViewModel.AcceptPalette).

    private void OnPaletteVisibilityChanged()
    {
        PaletteOverlay.Visibility = _vm.IsPaletteOpen ? Visibility.Visible : Visibility.Collapsed;

        if (!_vm.IsPaletteOpen) return;

        // Katman yeni görünür oldu; odak ancak yerleşimden sonra verilebilir.
        Dispatcher.BeginInvoke(() =>
        {
            PaletteQuery.Focus();
            PaletteQuery.SelectAll();
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    /// <summary>
    /// Klavye paletin TAMAMINI sürer: yazarken eller arama kutusunda kalsın
    /// diye gezinme de buradan yönetilir, listeye odak geçmez.
    /// </summary>
    private void PaletteQuery_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                _vm.ClosePalette();
                e.Handled = true;
                break;

            case Key.Enter:
                _vm.AcceptPalette();
                e.Handled = true;
                break;

            case Key.Down:
                _vm.Palette.MoveNext();
                PaletteResults.ScrollIntoView(_vm.Palette.Selected);
                e.Handled = true;
                break;

            case Key.Up:
                _vm.Palette.MovePrevious();
                PaletteResults.ScrollIntoView(_vm.Palette.Selected);
                e.Handled = true;
                break;
        }
    }

    private void PaletteResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        => _vm.AcceptPalette();

    /// <summary>
    /// Karartmaya tıklamak paleti kapatır. Yalnızca KARARTMANIN kendisine
    /// yapılan tıklama sayılır; paletin içine tıklamak kapatmamalı.
    /// </summary>
    private void PaletteOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, PaletteOverlay))
            _vm.ClosePalette();
    }

    // ── Sekme sağ tık menüsü (Faz E4) ─────────────────────────────────────
    //
    // Menü öğesinin DataContext'i, menünün açıldığı TabItem'dan miras kalır;
    // yani üzerinde sağ tıklanan SEKMEDİR. Toplu kapatma kararlarını yine
    // ShellViewModel verir — kapatılamaz sekme ve kaydedilmemiş değişiklik
    // kontrolü burada tekrarlanmaz.

    private void TabMenuClose_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ShellTab tab })
            _vm.CloseTab(tab);
    }

    private void TabMenuCloseOthers_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ShellTab tab })
            _vm.CloseOtherTabs(tab);
    }

    private void TabMenuCloseRight_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ShellTab tab })
            _vm.CloseTabsToTheRight(tab);
    }

    /// <summary>
    /// Kapatılamaz sekme (Nakit İşlemler) açık kalır — çıkıştaki
    /// CloseAllTabs'tan farkı budur.
    /// </summary>
    private void TabMenuCloseAll_Click(object sender, RoutedEventArgs e)
        => _vm.CloseClosableTabs();

    private static T? FindAncestor<T>(DependencyObject node) where T : DependencyObject
    {
        for (; node is not null; node = System.Windows.Media.VisualTreeHelper.GetParent(node))
            if (node is T match) return match;

        return null;
    }

    // ── Klavye kısayolları ────────────────────────────────────────────────

    private bool _forwarding;

    /// <summary>
    /// Kısayolu AKTİF SEKMEYE yönlendirir.
    ///
    /// Buraya yalnızca odak sekmenin DIŞINDAYKEN (navigasyon rayı, durum
    /// şeridi) gelinir: odak ekranın içindeyse komut ekranın kendi
    /// CommandBinding'inde işlenir ve olay buraya kadar yükselmez.
    ///
    /// Kabuk komutun ne yaptığını bilmez — yalnızca hedefi değiştirir.
    /// Aktif ekranın o komut için bağlaması yoksa <c>CanExecute</c> false
    /// döner ve hiçbir şey olmaz.
    /// </summary>
    private void Command_Forward(object sender, ExecutedRoutedEventArgs e)
    {
        // Yeniden giriş koruması: hedef ekranın bağlaması yoksa komut ağaçta
        // tekrar buraya kadar yükselebilirdi.
        if (_forwarding) return;

        if (e.Command is not RoutedCommand command) return;
        if (_vm.ActiveTab?.View is not IInputElement target) return;

        _forwarding = true;
        try
        {
            if (command.CanExecute(e.Parameter, target))
                command.Execute(e.Parameter, target);
        }
        finally
        {
            _forwarding = false;
        }
    }

    // ── Kişisel ayarlar ───────────────────────────────────────────────────

    /// <summary>
    /// Harf Duyarlılığı — kişisel tercih, yetki gerektirmez. Modal kalır;
    /// sekmeye dönüştürülmedi (bkz. docs/03-Modules/UserSettings.md).
    /// </summary>
    private void OpenTextCaseSettings_Click(object sender, RoutedEventArgs e)
    {
        new Settings.TextCaseSettingsWindow(_services) { Owner = this }.ShowDialog();
    }

    // ── Çıkış ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Onay verildi ve tüm sekmeler kapandı. Geriye denetim kaydı ve
    /// App.xaml.cs'in okuduğu sözleşme kaldı — MainWindow ile birebir aynı.
    /// </summary>
    private async void OnLogoutRequested()
    {
        await Common.SessionLogout.WriteAuditAsync(_services);

        IsLogoutRequested = true;
        Close();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Logout yolundan geliniyorsa sekmeler zaten kapatıldı.
        if (IsLogoutRequested) return;

        if (!_vm.CloseAllTabs()) e.Cancel = true;
    }
}
