using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.DeleteCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.QuickUpdateCargoStatus;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Label.GenerateCargoLabel;
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

        var listHandler        = services.GetRequiredService<GetCargoShipmentListHandler>();
        var quickStatusHandler = services.GetRequiredService<QuickUpdateCargoStatusHandler>();
        _vm = new CargoShipmentListViewModel(listHandler, quickStatusHandler, direction);
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
        CopyButton.Visibility   = manageVisibility;
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

    private async void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        var form = new CargoShipmentEditWindow(_services) { Owner = this };
        await form.PrepareCopyAsync(_vm.Selected);
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

    private async void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // InitializeComponent sırasında ComboBox ItemsSource bağlanınca SelectionChanged tetiklenir.
        // IsLoaded false iken henüz Loaded event ateşlenmemiştir; data yükü Loaded handler'a bırakılır.
        if (!IsLoaded) return;
        await _vm.LoadAsync();
    }

    private async void PriorityFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        await _vm.LoadAsync();
    }

    /// <summary>
    /// Seçili kargo için A6 PDF etiketi üretir ve sistem varsayılan PDF görüntüleyicisinde açar.
    /// Preview audit edilmez; gerçek baskı (PrintCargoLabel) Sprint 3.3+'ta eklenecek.
    /// </summary>
    private async void LabelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;

        var handler = _services.GetRequiredService<GenerateCargoLabelHandler>();
        var result  = await handler.HandleAsync(new GenerateCargoLabelRequest
        {
            Id        = _vm.Selected.Id,
            Direction = _vm.Direction
        });

        if (!result.Success)
        {
            _dialogService.ShowError(result.ErrorMessage ?? "Etiket oluşturulamadı.");
            return;
        }

        // Geçici dosyaya yaz ve sistem PDF görüntüleyicisinde aç
        var safeName = (_vm.Selected.ShipmentNumber ?? _vm.Selected.Id.ToString()[..8])
            .Replace('/', '-').Replace('\\', '-');
        var tempPath = Path.Combine(Path.GetTempPath(), $"kargo-etiketi-{safeName}.pdf");
        await File.WriteAllBytesAsync(tempPath, result.Data!);
        Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
    }

    /// <summary>Seçili kargonun TrackingUrl'ini default tarayıcıda açar.</summary>
    private void TrackButton_Click(object sender, RoutedEventArgs e)
    {
        var url = _vm.Selected?.TrackingUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            _dialogService.ShowWarning("Bu kargo için takip linki bulunmamaktadır.", "Takip Et");
            return;
        }
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    /// <summary>QuickUpdateStatusDialog ile durum değiştirme.</summary>
    private async void QuickStatusButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        var dto = _vm.Selected;

        var dialog = new QuickUpdateStatusDialog(dto.StatusDisplay, dto.Status)
        {
            Owner = this
        };
        if (dialog.ShowDialog() != true || dialog.SelectedStatus is null) return;

        var userContext = _services.GetRequiredService<IUserContext>();
        var (success, error) = await _vm.QuickUpdateStatusAsync(
            dto.Id, dialog.SelectedStatus.Value, userContext.UserId);

        if (!success)
            _dialogService.ShowError(error ?? "Beklenmeyen bir hata oluştu.");
        else
            await _vm.LoadAsync();
    }

    /// <summary>Takip linkine tıklandığında default tarayıcıda açar.</summary>
    private void TrackingUrl_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        if (e.Uri is not null)
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
