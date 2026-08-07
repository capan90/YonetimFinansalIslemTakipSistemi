using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Permissions;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Permissions;

public partial class UserPermissionScreen : UserControl
{
    private readonly UserPermissionViewModel _vm;

    public UserPermissionScreen(IServiceProvider services)
    {
        InitializeComponent();
        _vm       = services.GetRequiredService<UserPermissionViewModel>();
        DataContext = _vm;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
        => await _vm.LoadAsync();
}
