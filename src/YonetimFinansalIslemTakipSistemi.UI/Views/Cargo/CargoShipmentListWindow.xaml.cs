using System.Windows;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

/// <summary>
/// Kargo listesi — ince barındırıcı (Faz D6).
///
/// İçeriğin tamamı <see cref="CargoShipmentListScreen"/>'de. Bu pencere
/// yalnızca pencere-seviyesi özellikleri taşır ve ekranın kapanma isteğini
/// kendini kapatarak karşılar; kabukta aynı istek sekmeyi kapatır.
///
/// SİLİNMEDİ: MainWindow ve Kargo Dashboard menüleri hâlâ bu pencereyi açıyor.
/// </summary>
public partial class CargoShipmentListWindow : Window
{
    public CargoShipmentListWindow(IServiceProvider services, CargoShipmentDirection direction)
    {
        InitializeComponent();

        var screen = new CargoShipmentListScreen(services, direction);
        screen.CloseRequested += Close;

        Title              = screen.ScreenTitle;
        ScreenHost.Content = screen;
    }
}
