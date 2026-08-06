using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
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

        // Varsayılan sekme. Yetkisiz kullanıcıda OpenScreen null döner ve kabuk
        // sekmesiz açılır — burada ayrıca yetki kontrolü YAPILMAZ, tek kapı
        // ShellViewModel.Resolve'dur (iki yerde yetki mantığı tutulmaz).
        var opened = _vm.OpenScreen(ScreenKey.CashTransactions);
        if (opened is not null)
            _vm.SelectedNavigationItem = opened.Definition;

        // Pencere X ile kapatılırsa da açık ekranlara söz hakkı verilir;
        // kaydedilmemiş değişiklik sessizce kaybolmasın.
        Closing += OnClosing;
    }

    // ── Navigasyon ────────────────────────────────────────────────────────

    private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Zaten açık ekran ikinci sekme üretmez; ShellViewModel mevcut sekmeye
        // odaklanır (bkz. OpenScreen).
        if (_vm.SelectedNavigationItem is { } screen)
            _vm.OpenScreen(screen.Key);
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
