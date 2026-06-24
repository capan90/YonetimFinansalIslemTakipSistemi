using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;

/// <summary>
/// Hem gelen hem giden kargo listesi için kullanılır; Direction property ile ayrışır.
/// </summary>
public class CargoShipmentListViewModel : INotifyPropertyChanged
{
    private readonly GetCargoShipmentListHandler _listHandler;

    private string _keyword = string.Empty;
    private CargoShipmentDto? _selected;
    private ObservableCollection<CargoShipmentDto> _items = [];

    public CargoShipmentDirection Direction { get; }

    public string Keyword
    {
        get => _keyword;
        set { _keyword = value; OnPropertyChanged(); }
    }

    public CargoShipmentDto? Selected
    {
        get => _selected;
        set { _selected = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSelected)); }
    }

    public bool HasSelected => _selected is not null;

    public ObservableCollection<CargoShipmentDto> Items
    {
        get => _items;
        private set { _items = value; OnPropertyChanged(); }
    }

    public ICommand SearchCommand { get; }

    public CargoShipmentListViewModel(
        GetCargoShipmentListHandler listHandler,
        CargoShipmentDirection direction)
    {
        _listHandler  = listHandler;
        Direction     = direction;
        SearchCommand = new RelayCommand(async () => await LoadAsync());
    }

    public async Task LoadAsync()
    {
        var result = await _listHandler.HandleAsync(new GetCargoShipmentListQuery
        {
            Direction = Direction,
            Keyword   = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword
        });

        Items = new ObservableCollection<CargoShipmentDto>(result);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
