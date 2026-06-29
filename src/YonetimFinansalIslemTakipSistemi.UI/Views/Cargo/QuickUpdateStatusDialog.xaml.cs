using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

public partial class QuickUpdateStatusDialog : Window
{
    private readonly CargoShipmentDirection _direction;

    public CargoShipmentStatus? SelectedStatus { get; private set; }

    public QuickUpdateStatusDialog(
        string currentStatusDisplay,
        CargoShipmentStatus currentStatus,
        CargoShipmentDirection direction = CargoShipmentDirection.Outgoing)
    {
        InitializeComponent();
        _direction = direction;
        CurrentStatusText.Text = currentStatusDisplay;

        // Mevcut durum ve izin verilen sonraki durumlar listelenir; Taslak gösterilmez
        var allowed = CargoStatusTransitions.GetAllowedNext(currentStatus, direction)
            .Where(s => s != CargoShipmentStatus.Draft)
            .ToList();

        NewStatusCombo.ItemsSource = allowed
            .Select(s => new { Display = DisplayStatus(s), Value = s })
            .ToList();

        NewStatusCombo.SelectedIndex = 0;
    }

    private void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (NewStatusCombo.SelectedValue is not CargoShipmentStatus status) return;
        SelectedStatus = status;
        DialogResult   = true;
    }

    private string DisplayStatus(CargoShipmentStatus s) => s switch
    {
        CargoShipmentStatus.Draft              => _direction == CargoShipmentDirection.Incoming ? "Bekleniyor" : "Gönderime Hazır",
        CargoShipmentStatus.Prepared           => "Gönderime Hazır",
        CargoShipmentStatus.HandedToCargo      => "Kargoya Teslim Edildi",
        CargoShipmentStatus.Shipped            => "Gönderildi",
        CargoShipmentStatus.Waiting            => "Bekleniyor",
        CargoShipmentStatus.Received           => "Teslim Alındı",
        CargoShipmentStatus.PersonnelDelivered => "Personele Teslim Edildi",
        CargoShipmentStatus.Delivered          => "Teslim Edildi",
        CargoShipmentStatus.Cancelled          => "İptal",
        _                                      => s.ToString()
    };
}
