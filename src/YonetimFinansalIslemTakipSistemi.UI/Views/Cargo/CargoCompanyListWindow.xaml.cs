using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Commands.DeleteCargoCompany;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

public partial class CargoCompanyListWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly CargoCompanyListViewModel _vm;
    private readonly IDialogService _dialogService;

    public CargoCompanyListWindow(IServiceProvider services)
    {
        InitializeComponent();
        _services      = services;
        _vm            = services.GetRequiredService<CargoCompanyListViewModel>();
        _dialogService = services.GetRequiredService<IDialogService>();
        DataContext    = _vm;

        // UI gizlemesi; asıl koruma handler seviyesindedir
        var userContext = services.GetRequiredService<IUserContext>();
        var canManage = userContext.HasPermission(PermissionType.CanManageCargoCompanies);
        var manageVisibility = canManage ? Visibility.Visible : Visibility.Collapsed;
        NewButton.Visibility    = manageVisibility;
        EditButton.Visibility   = manageVisibility;
        DeleteButton.Visibility = manageVisibility;

        Loaded += async (_, _) => await _vm.LoadAsync();
    }

    private async void NewButton_Click(object sender, RoutedEventArgs e)
    {
        var form = new CargoCompanyEditWindow(_services) { Owner = this };
        if (form.ShowDialog() == true) await _vm.LoadAsync();
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        var form = new CargoCompanyEditWindow(_services) { Owner = this };
        form.InitializeForEdit(_vm.Selected);
        if (form.ShowDialog() == true) await _vm.LoadAsync();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        if (!_dialogService.ShowConfirmation(
                $"'{_vm.Selected.Name}' firmasını silmek istediğinize emin misiniz?", "Kargo Firması Sil"))
            return;

        var handler     = _services.GetRequiredService<DeleteCargoCompanyHandler>();
        var userContext = _services.GetRequiredService<IUserContext>();

        var result = await handler.HandleAsync(new DeleteCargoCompanyRequest
        {
            Id              = _vm.Selected.Id,
            DeletedByUserId = userContext.UserId
        });

        if (!result.Success)
            _dialogService.ShowError(result.ErrorMessage ?? "Beklenmeyen bir hata oluştu.");
        else
            await _vm.LoadAsync();
    }

    private async void MainGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_vm.Selected is null) return;
        var form = new CargoCompanyEditWindow(_services) { Owner = this };
        form.InitializeForEdit(_vm.Selected);
        if (form.ShowDialog() == true) await _vm.LoadAsync();
    }

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await _vm.LoadAsync();
    }
}
