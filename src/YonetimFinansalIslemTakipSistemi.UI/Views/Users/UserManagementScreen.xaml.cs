using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Users;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Users;

public partial class UserManagementScreen : UserControl
{
    private readonly IServiceProvider _services;
    private readonly UserManagementViewModel _viewModel;

    public UserManagementScreen(IServiceProvider services)
    {
        InitializeComponent();
        _services  = services;
        _viewModel = services.GetRequiredService<UserManagementViewModel>();
        DataContext = _viewModel;

        ScreenData.Bind(this, () => _viewModel.LoadAsync());
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var form = new UserFormWindow(_services);
        if (form.ShowDialog() == true)
            await _viewModel.LoadAsync();
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedUser is null) return;
        var form = new UserFormWindow(_services, _viewModel.SelectedUser);
        if (form.ShowDialog() == true)
            await _viewModel.LoadAsync();
    }
}
