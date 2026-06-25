using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

public partial class QuickUpdateStatusDialog : Window
{
    public CargoShipmentStatus? SelectedStatus { get; private set; }

    public QuickUpdateStatusDialog(string currentStatusDisplay, CargoShipmentStatus currentStatus)
    {
        InitializeComponent();
        CurrentStatusText.Text = currentStatusDisplay;

        var allowed = CargoStatusTransitions.GetAllowedNext(currentStatus);
        NewStatusCombo.ItemsSource = allowed
            .Select(s => new { Display = DisplayStatus(s), Value = s })
            .ToList();

        NewStatusCombo.SelectedIndex = 0;
    }

    private void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (NewStatusCombo.SelectedValue is not CargoShipmentStatus status) return;
        SelectedStatus  = status;
        DialogResult    = true;
    }

    private static string DisplayStatus(CargoShipmentStatus s) => s switch
    {
        CargoShipmentStatus.Draft     => "Taslak",
        CargoShipmentStatus.Prepared  => "Hazırlandı",
        CargoShipmentStatus.Shipped   => "Gönderildi",
        CargoShipmentStatus.Received  => "Alındı",
        CargoShipmentStatus.Delivered => "Teslim Edildi",
        CargoShipmentStatus.Cancelled => "İptal",
        _                             => s.ToString()
    };
}
