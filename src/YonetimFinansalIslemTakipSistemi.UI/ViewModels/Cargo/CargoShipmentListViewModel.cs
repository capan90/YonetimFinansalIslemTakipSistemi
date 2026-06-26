using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.QuickUpdateCargoStatus;
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
    private readonly QuickUpdateCargoStatusHandler _quickStatusHandler;

    private string _keyword             = string.Empty;
    private string _selectedSearchType  = "Genel";
    private string _selectedStatusFilter   = "(Tümü)";
    private string _selectedPriorityFilter = "(Tümü)";
    private CargoShipmentDto? _selected;
    private ObservableCollection<CargoShipmentDto> _items = [];

    public CargoShipmentDirection Direction { get; }

    public string Keyword
    {
        get => _keyword;
        set { _keyword = value; OnPropertyChanged(); }
    }

    public string SelectedSearchType
    {
        get => _selectedSearchType;
        set { _selectedSearchType = value; OnPropertyChanged(); }
    }

    public IReadOnlyList<string> SearchTypeOptions { get; } =
        ["Genel", "Firma", "Kargo No", "Takip No", "Araç Plakası"];

    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set { _selectedStatusFilter = value; OnPropertyChanged(); }
    }

    public string SelectedPriorityFilter
    {
        get => _selectedPriorityFilter;
        set { _selectedPriorityFilter = value; OnPropertyChanged(); }
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

    public IReadOnlyList<string> StatusFilterOptions { get; } =
        ["(Tümü)", "Taslak", "Hazırlandı", "Gönderildi", "Alındı", "Teslim Edildi", "İptal"];

    public IReadOnlyList<string> PriorityFilterOptions { get; } =
        ["(Tümü)", "Normal", "Orta", "Acil", "Çok Acil"];

    public ICommand SearchCommand { get; }

    public CargoShipmentListViewModel(
        GetCargoShipmentListHandler listHandler,
        QuickUpdateCargoStatusHandler quickStatusHandler,
        CargoShipmentDirection direction)
    {
        _listHandler        = listHandler;
        _quickStatusHandler = quickStatusHandler;
        Direction           = direction;
        SearchCommand       = new RelayCommand(async () => await LoadAsync());
    }

    public async Task LoadAsync()
    {
        var result = await _listHandler.HandleAsync(new GetCargoShipmentListQuery
        {
            Direction  = Direction,
            Keyword    = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword,
            SearchType = SelectedSearchType == "Genel" ? null : SelectedSearchType,
            Status     = ParseStatusFilter(SelectedStatusFilter),
            Priority   = ParsePriorityFilter(SelectedPriorityFilter)
        });

        Items = new ObservableCollection<CargoShipmentDto>(result);
    }

    public async Task<(bool success, string? error)> QuickUpdateStatusAsync(
        Guid id, CargoShipmentStatus newStatus, Guid updatedByUserId)
    {
        var req = new QuickUpdateCargoStatusRequest
        {
            Id              = id,
            Direction       = Direction,
            NewStatus       = newStatus,
            UpdatedByUserId = updatedByUserId
        };
        var result = await _quickStatusHandler.HandleAsync(req);
        return (result.Success, result.ErrorMessage);
    }

    private static CargoShipmentStatus? ParseStatusFilter(string display) => display switch
    {
        "Taslak"        => CargoShipmentStatus.Draft,
        "Hazırlandı"    => CargoShipmentStatus.Prepared,
        "Gönderildi"    => CargoShipmentStatus.Shipped,
        "Alındı"        => CargoShipmentStatus.Received,
        "Teslim Edildi" => CargoShipmentStatus.Delivered,
        "İptal"         => CargoShipmentStatus.Cancelled,
        _               => null
    };

    private static CargoShipmentPriority? ParsePriorityFilter(string display) => display switch
    {
        "Normal"    => CargoShipmentPriority.Normal,
        "Orta"      => CargoShipmentPriority.Medium,
        "Acil"      => CargoShipmentPriority.Urgent,
        "Çok Acil"  => CargoShipmentPriority.Critical,
        _           => null
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
