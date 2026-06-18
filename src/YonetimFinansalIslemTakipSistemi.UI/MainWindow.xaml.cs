using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;
using YonetimFinansalIslemTakipSistemi.UI.Views.Users;

namespace YonetimFinansalIslemTakipSistemi.UI;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _services;

    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        var vm = services.GetRequiredService<CashTransactionListViewModel>();
        DataContext = vm;

        // İlk açılışta tüm kayıtları yükle
        Loaded += async (_, _) => await vm.LoadAsync();
    }

    private void OpenUserManagement_Click(object sender, RoutedEventArgs e)
    {
        var win = new UserManagementWindow(_services) { Owner = this };
        win.ShowDialog();
    }
}
