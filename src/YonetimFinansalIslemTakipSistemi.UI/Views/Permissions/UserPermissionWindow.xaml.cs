using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Permissions;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Permissions;

public partial class UserPermissionWindow : Window
{
    private readonly UserPermissionViewModel _vm;

    public UserPermissionWindow(IServiceProvider services)
    {
        InitializeComponent();
        _vm       = services.GetRequiredService<UserPermissionViewModel>();
        DataContext = _vm;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
        => await _vm.LoadAsync();
}
