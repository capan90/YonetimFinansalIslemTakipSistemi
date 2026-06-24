using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

public partial class CargoShipmentEditWindow : Window
{
    private readonly CargoShipmentEditViewModel _vm;

    public CargoShipmentEditWindow(IServiceProvider services)
    {
        InitializeComponent();
        _vm = services.GetRequiredService<CargoShipmentEditViewModel>();
        DataContext = _vm;
        _vm.SaveCompleted += () => { DialogResult = true; Close(); };
    }

    /// <summary>Yeni kayıt için: yön belirle ve lookup'ları yükle.</summary>
    public async Task PrepareNewAsync(CargoShipmentDirection direction)
    {
        _vm.SetDirection(direction);
        await _vm.LoadLookupsAsync();
    }

    /// <summary>Düzenleme için: mevcut kaydı ViewModel'e aktar.</summary>
    public async Task PrepareEditAsync(CargoShipmentDto dto)
    {
        await _vm.InitializeAsync(dto);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
