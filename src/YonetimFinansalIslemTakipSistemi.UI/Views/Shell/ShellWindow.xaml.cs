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

        // Açılışta güncelleme kontrolü — uygulama seviyesi bir iş ve artık
        // kabuğun sorumluluğu. Eskiden MainWindow ve Kargo Panosu ayrı ayrı
        // yürütüyordu; kabuk ikisinin de yerini alınca tek yerde kaldı.
        // Ekran yüklendikten sonra, kullanıcıyı bloklamadan çalışır.
        Loaded += async (_, _) =>
            await Services.StartupUpdateChecker.RunOnceAsync(_services, _dialogService);
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
        if (e.AddedItems.Count == 0) return;
        if (e.AddedItems[0] is not ScreenDefinition screen) return;

        // Ray gruplara bölündüğü için her grubun kendi ListBox'ı var; birinden
        // seçim yapılınca diğerlerinin seçimi temizlenmeli, yoksa rayda birden
        // çok "seçili" öğe görünür.
        ClearOtherSelections(sender);

        _vm.SelectedNavigationItem = screen;

        // Zaten açık ekran ikinci sekme üretmez; ShellViewModel mevcut sekmeye
        // odaklanır (bkz. OpenScreen).
        _vm.OpenScreen(screen.Key);
    }

    private void ClearOtherSelections(object current)
    {
        foreach (var list in Descendants(this).OfType<ListBox>())
            if (!ReferenceEquals(list, current))
                list.SelectedItem = null;
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
