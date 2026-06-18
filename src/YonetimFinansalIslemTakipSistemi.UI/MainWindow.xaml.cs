using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;

namespace YonetimFinansalIslemTakipSistemi.UI;

public partial class MainWindow : Window
{
    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        var vm = services.GetRequiredService<CashTransactionListViewModel>();
        DataContext = vm;

        // İlk açılışta tüm kayıtları yükle
        Loaded += async (_, _) => await vm.LoadAsync();
    }
}
