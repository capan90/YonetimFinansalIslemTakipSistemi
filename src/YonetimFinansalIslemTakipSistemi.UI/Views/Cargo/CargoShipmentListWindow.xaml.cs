using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.DeleteCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

public partial class CargoShipmentListWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly CargoShipmentListViewModel _vm;
    private readonly IDialogService _dialogService;

    public CargoShipmentListWindow(IServiceProvider services, CargoShipmentDirection direction)
    {
        InitializeComponent();
        _services      = services;
        _dialogService = services.GetRequiredService<IDialogService>();

        var listHandler = services.GetRequiredService<GetCargoShipmentListHandler>();
        _vm = new CargoShipmentListViewModel(listHandler, direction);
        DataContext = _vm;

        Title = direction == CargoShipmentDirection.Incoming ? "Gelen Kargolar" : "Giden Kargolar";
        TitleBlock.Text = Title;

        // UI gizlemesi; asıl koruma handler seviyesindedir
        var userContext = services.GetRequiredService<IUserContext>();
        var managePermission = direction == CargoShipmentDirection.Incoming
            ? PermissionType.CanManageIncomingCargo
            : PermissionType.CanManageOutgoingCargo;
        var manageVisibility = userContext.HasPermission(managePermission)
            ? Visibility.Visible : Visibility.Collapsed;
        NewButton.Visibility    = manageVisibility;
        EditButton.Visibility   = manageVisibility;
        DeleteButton.Visibility = manageVisibility;

        Loaded += async (_, _) => await _vm.LoadAsync();
    }

    private async void NewButton_Click(object sender, RoutedEventArgs e)
    {
        var form = new CargoShipmentEditWindow(_services) { Owner = this };
        await form.PrepareNewAsync(_vm.Direction);
        if (form.ShowDialog() == true) await _vm.LoadAsync();
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        var form = new CargoShipmentEditWindow(_services) { Owner = this };
        await form.PrepareEditAsync(_vm.Selected);
        if (form.ShowDialog() == true) await _vm.LoadAsync();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        var label = string.IsNullOrWhiteSpace(_vm.Selected.ShipmentNumber)
            ? "seçili kargo kaydını"
            : $"'{_vm.Selected.ShipmentNumber}' kargo kaydını";

        if (!_dialogService.ShowConfirmation(
                $"{label} silmek istediğinize emin misiniz?", "Kargo Sil"))
            return;

        var handler     = _services.GetRequiredService<DeleteCargoShipmentHandler>();
        var userContext = _services.GetRequiredService<IUserContext>();

        var result = await handler.HandleAsync(new DeleteCargoShipmentRequest
        {
            Id              = _vm.Selected.Id,
            Direction       = _vm.Direction,
            DeletedByUserId = userContext.UserId
        });

        if (!result.Success)
            _dialogService.ShowError(result.ErrorMessage ?? "Beklenmeyen bir hata oluştu.");
        else
            await _vm.LoadAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        => await _vm.LoadAsync();

    private async void MainGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_vm.Selected is null) return;
        var form = new CargoShipmentEditWindow(_services) { Owner = this };
        await form.PrepareEditAsync(_vm.Selected);
        if (form.ShowDialog() == true) await _vm.LoadAsync();
    }

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await _vm.LoadAsync();
    }
}
