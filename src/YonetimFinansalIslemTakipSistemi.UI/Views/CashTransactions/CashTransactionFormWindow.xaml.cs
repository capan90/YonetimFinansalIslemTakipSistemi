using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCashTransactions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.CashTransactions;

public partial class CashTransactionFormWindow : Window
{
    private readonly CashTransactionFormViewModel _vm;

    public CashTransactionFormWindow(IServiceProvider services)
    {
        InitializeComponent();
        _vm              = services.GetRequiredService<CashTransactionFormViewModel>();
        DataContext      = _vm;
        _vm.SaveCompleted = () => { DialogResult = true; };
    }

    /// <summary>
    /// Düzenleme modunda açmak için ShowDialog() öncesi çağrılır.
    /// </summary>
    public void InitializeForEdit(CashTransactionDto dto) => _vm.Initialize(dto);

    private void CancelButton_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}
