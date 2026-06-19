using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.DeleteCashTransaction;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
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
        _services   = services;
        _listVm     = services.GetRequiredService<CashTransactionListViewModel>();
        DataContext = _listVm;

        Loaded += async (_, _) => await _listVm.LoadAsync();
    }

    private async void NewTransactionButton_Click(object sender, RoutedEventArgs e)
    {
        var form = new CashTransactionFormWindow(_services) { Owner = this };
        if (form.ShowDialog() == true)
            await _listVm.LoadAsync();
    }

    private async void EditTransactionButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = _listVm.SelectedTransaction;
        if (selected is null) return;

        var form = new CashTransactionFormWindow(_services) { Owner = this };
        form.InitializeForEdit(selected);
        if (form.ShowDialog() == true)
            await _listVm.LoadAsync();
    }

    private async void DeleteTransactionButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = _listVm.SelectedTransaction;
        if (selected is null) return;

        var label    = string.IsNullOrWhiteSpace(selected.Description) ? "seçili işlemi" : $"'{selected.Description}'";
        var confirm  = MessageBox.Show(
            $"{label} silmek istediğinize emin misiniz?",
            "İşlem Sil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        var handler     = _services.GetRequiredService<DeleteCashTransactionHandler>();
        var userContext = _services.GetRequiredService<IUserContext>();

        var request = new DeleteCashTransactionRequest
        {
            Id              = selected.Id,
            DeletedByUserId = userContext.UserId
        };

        var result = await handler.HandleAsync(request);
        if (!result.Success)
        {
            MessageBox.Show(result.ErrorMessage, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        await _listVm.LoadAsync();
    }

    private void OpenUserManagement_Click(object sender, RoutedEventArgs e)
    {
        var win = new UserManagementWindow(_services) { Owner = this };
        win.ShowDialog();
    }
}
