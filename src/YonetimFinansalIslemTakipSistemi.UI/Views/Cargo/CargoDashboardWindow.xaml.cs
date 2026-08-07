using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

/// <summary>
/// Kargo Dashboard — ince barındırıcı (Faz D6).
///
/// İçeriğin tamamı <see cref="CargoDashboardScreen"/>'de. Bu pencere yalnızca
/// KABUK sorumluluklarını tutar: çıkış onayı + audit + <see cref="IsLogoutRequested"/>
/// ve pencerenin kapatılması.
///
/// SİLİNMEDİ: cargo başlangıç akışı ShellWindow'a taşındıktan sonra da geri
/// dönüş yolu açık kalsın diye duruyor. Ekran içindeki navigasyon şeridi ve
/// yardım menüsü burada barındığında ÇALIŞMAYA DEVAM EDER; kabukta ise
/// gizlenir ve yerini kabuğun navigasyon rayı alır.
/// </summary>
public partial class CargoDashboardWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly IDialogService   _dialogService;

    /// <summary>App.xaml.cs'in okuduğu oturum sözleşmesi — değişmedi.</summary>
    public bool IsLogoutRequested { get; private set; }

    public CargoDashboardWindow(IServiceProvider services)
    {
        InitializeComponent();

        _services      = services;
        _dialogService = services.GetRequiredService<IDialogService>();

        var screen = new CargoDashboardScreen(services);
        screen.LogoutRequested += OnLogoutRequested;
        screen.CloseRequested  += Close;

        ScreenHost.Content = screen;
    }

    private async void OnLogoutRequested()
    {
        if (!Common.SessionLogout.Confirm(_dialogService)) return;

        await Common.SessionLogout.WriteAuditAsync(_services);

        IsLogoutRequested = true;
        Close();
    }
}
