using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;
using YonetimFinansalIslemTakipSistemi.UI.Views.CashTransactions;
using YonetimFinansalIslemTakipSistemi.UI.Views.Users;

namespace YonetimFinansalIslemTakipSistemi.UI;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly CashTransactionListViewModel _listVm;

    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        _listVm   = services.GetRequiredService<CashTransactionListViewModel>();
        DataContext = _listVm;

        Loaded += async (_, _) => await _listVm.LoadAsync();
    }

    private async void NewTransactionButton_Click(object sender, RoutedEventArgs e)
    {
        var form = new CashTransactionFormWindow(_services) { Owner = this };
        if (form.ShowDialog() == true)
            await _listVm.LoadAsync();
    }

    private void OpenUserManagement_Click(object sender, RoutedEventArgs e)
    {
        var win = new UserManagementWindow(_services) { Owner = this };
        win.ShowDialog();
    }
}
