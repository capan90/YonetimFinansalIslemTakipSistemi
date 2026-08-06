using System.Reflection;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Shell;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Shell;

/// <summary>
/// Tek kabuk penceresi — İSKELET.
///
/// HENÜZ BAŞLANGIÇ EKRANI DEĞİL: App.xaml.cs hâlâ MainWindow ve
/// CargoDashboardWindow'u açıyor, kullanıcının akışı değişmedi. Bu pencere
/// ekranlar UserControl'e taşındıkça devreye girecek.
///
/// ÇIKIŞ SÖZLEŞMESİ mevcut kabuklarla BİREBİR aynı:
/// <see cref="IsLogoutRequested"/> + <c>Close()</c>. App.xaml.cs'teki
/// login → kabuk → logout → login döngüsü hiç değişmeden çalışır.
/// </summary>
public partial class ShellWindow : Window
{
    private readonly ShellViewModel _vm;

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

        var userContext = services.GetRequiredService<IUserContext>();

        _vm = new ShellViewModel(services, userContext, screens);
        _vm.LogoutRequested += OnLogoutRequested;

        DataContext = _vm;

        StatusUserText.Text    = userContext.FullName;
        StatusVersionText.Text = $"Sürüm {Assembly.GetExecutingAssembly().GetName().Version}";

        // Pencere X ile kapatılırsa da açık ekranlara söz hakkı verilir;
        // kaydedilmemiş değişiklik sessizce kaybolmasın.
        Closing += OnClosing;
    }

    private void OnLogoutRequested()
    {
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
