using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.CashTransactions;

public partial class CashTransactionFormWindow : Window
{
    public CashTransactionFormWindow(IServiceProvider services)
    {
        InitializeComponent();
        var vm = services.GetRequiredService<CashTransactionFormViewModel>();
        DataContext  = vm;
        vm.SaveCompleted = () => { DialogResult = true; };
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}
