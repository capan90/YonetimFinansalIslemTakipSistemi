using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;

using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

#region Legacy - Shell Migration

/// <summary>
/// Operasyon Merkezi — ince barındırıcı (Faz D6).
///
/// İçeriğin tamamı <see cref="CargoOperationCenterScreen"/>'de.
///
/// <see cref="WasModified"/> SÖZLEŞMESİ KORUNDU: kargo listesi bu pencereyi
/// modal açtığında kapanış sonrası bu değere bakıp listeyi yeniliyor. Kabukta
/// operasyon merkezi ayrı bir sekme olduğu için orada tazeleme sekmeye geri
/// dönüldüğünde yapılır (bkz. CargoShipmentListScreen).
/// </summary>
[Obsolete(LegacyShellMigration.Reason)]
public partial class CargoOperationCenterWindow : Window
{
    private readonly CargoOperationCenterScreen _screen;

    /// <summary>Status değiştirildi veya bildirim hazırlandıysa true.</summary>
    public bool WasModified => _screen.WasModified;

    public CargoOperationCenterWindow(IServiceProvider services, CargoShipmentDto dto)
    {
        InitializeComponent();

        _screen = new CargoOperationCenterScreen(services, dto);
        _screen.CloseRequested += Close;

        Title              = _screen.ScreenTitle;
        ScreenHost.Content = _screen;
    }
}

#endregion
