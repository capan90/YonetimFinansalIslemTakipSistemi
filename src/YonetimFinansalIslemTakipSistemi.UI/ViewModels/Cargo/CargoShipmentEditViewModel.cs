using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Queries.GetCargoCompanyList;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Queries.GetCompanyDirectoryList;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.CreateCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.UpdateCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;

public class CargoShipmentEditViewModel : INotifyPropertyChanged
{
    private readonly CreateCargoShipmentHandler _createHandler;
    private readonly UpdateCargoShipmentHandler _updateHandler;
    private readonly GetCargoCompanyListHandler _cargoCompanyListHandler;
    private readonly GetCompanyDirectoryListHandler _directoryListHandler;
    private readonly IUserContext _userContext;

    private Guid? _editTargetId;
    private CargoShipmentDirection _direction;
    // Düzenleme sırasında mevcut bildirim durumunu korur; UI'da değiştirilemiyor (Sprint 2)
    private CargoNotificationStatus _notificationStatus = CargoNotificationStatus.NotNotified;
    private DateTime _shipmentDate = DateTime.Today;
    private string _shipmentNumber = string.Empty;
    private string _senderName    = string.Empty;
    private string _receiverName  = string.Empty;
    private string _deliveredBy   = string.Empty;
    private string _receivedBy    = string.Empty;
    private string _vehiclePlate  = string.Empty;
    private string _trackingNumber = string.Empty;
    private string _notes          = string.Empty;
    private CargoCompanyDto? _selectedCargoCompany;
    private CompanyDirectoryDto? _selectedCompanyDirectory;
    private string _selectedShipmentType  = "Evrak";
    private string _selectedStatus        = "Taslak";
    private string? _errorMessage;

    public bool IsEditMode { get; private set; }
    public CargoShipmentDirection Direction => _direction;
    public string WindowTitle => IsEditMode
        ? (_direction == CargoShipmentDirection.Incoming ? "Gelen Kargo Düzenle" : "Giden Kargo Düzenle")
        : (_direction == CargoShipmentDirection.Incoming ? "Yeni Gelen Kargo"    : "Yeni Giden Kargo");

    public DateTime ShipmentDate  { get => _shipmentDate;   set { _shipmentDate  = value; OnPropertyChanged(); } }
    public string ShipmentNumber  { get => _shipmentNumber; set { _shipmentNumber = value; OnPropertyChanged(); } }
    public string SenderName      { get => _senderName;     set { _senderName    = value; OnPropertyChanged(); } }
    public string ReceiverName    { get => _receiverName;   set { _receiverName  = value; OnPropertyChanged(); } }
    public string DeliveredBy     { get => _deliveredBy;    set { _deliveredBy   = value; OnPropertyChanged(); } }
    public string ReceivedBy      { get => _receivedBy;     set { _receivedBy    = value; OnPropertyChanged(); } }
    public string VehiclePlate    { get => _vehiclePlate;   set { _vehiclePlate  = value; OnPropertyChanged(); } }
    public string TrackingNumber  { get => _trackingNumber; set { _trackingNumber = value; OnPropertyChanged(); } }
    public string Notes           { get => _notes;          set { _notes          = value; OnPropertyChanged(); } }

    public CargoCompanyDto? SelectedCargoCompany
    {
        get => _selectedCargoCompany;
        set { _selectedCargoCompany = value; OnPropertyChanged(); }
    }

    public CompanyDirectoryDto? SelectedCompanyDirectory
    {
        get => _selectedCompanyDirectory;
        set
        {
            _selectedCompanyDirectory = value;
            OnPropertyChanged();
            // Giden kargoda firma seçilince adres/iletişim bilgileri otomatik dolar
            if (value is not null && _direction == CargoShipmentDirection.Outgoing)
                FillFromDirectory(value);
        }
    }

    public string SelectedShipmentType
    {
        get => _selectedShipmentType;
        set { _selectedShipmentType = value; OnPropertyChanged(); }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set { _selectedStatus = value; OnPropertyChanged(); }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public ObservableCollection<CargoCompanyDto> CargoCompanies { get; } = [];
    public ObservableCollection<CompanyDirectoryDto> CompanyDirectories { get; } = [];

    public IReadOnlyList<string> ShipmentTypeOptions { get; } =
        ["Evrak", "Numune", "Fatura", "Sözleşme", "Yedek Parça", "Diğer"];

    public IReadOnlyList<string> StatusOptions { get; } =
        ["Taslak", "Hazırlandı", "Gönderildi", "Alındı", "Teslim Edildi", "İptal"];

    public Action? SaveCompleted { get; set; }
    public ICommand SaveCommand { get; }

    public CargoShipmentEditViewModel(
        CreateCargoShipmentHandler createHandler,
        UpdateCargoShipmentHandler updateHandler,
        GetCargoCompanyListHandler cargoCompanyListHandler,
        GetCompanyDirectoryListHandler directoryListHandler,
        IUserContext userContext)
    {
        _createHandler          = createHandler;
        _updateHandler          = updateHandler;
        _cargoCompanyListHandler = cargoCompanyListHandler;
        _directoryListHandler   = directoryListHandler;
        _userContext            = userContext;
        SaveCommand             = new RelayCommand(async () => await ExecuteSaveAsync());
    }

    public void SetDirection(CargoShipmentDirection direction)
    {
        _direction = direction;
        OnPropertyChanged(nameof(Direction));
        OnPropertyChanged(nameof(WindowTitle));
    }

    public async Task LoadLookupsAsync()
    {
        var companies = await _cargoCompanyListHandler.HandleAsync(
            new GetCargoCompanyListQuery { IsActive = true });
        CargoCompanies.Clear();
        foreach (var c in companies)
            CargoCompanies.Add(c);

        var dirs = await _directoryListHandler.HandleAsync(
            new GetCompanyDirectoryListQuery { IsActive = true });
        CompanyDirectories.Clear();
        foreach (var d in dirs)
            CompanyDirectories.Add(d);
    }

    public async Task InitializeAsync(CargoShipmentDto dto)
    {
        IsEditMode     = true;
        _editTargetId  = dto.Id;
        _direction     = dto.Direction;
        ShipmentDate   = dto.ShipmentDate;
        ShipmentNumber = dto.ShipmentNumber ?? string.Empty;
        SenderName     = dto.SenderName     ?? string.Empty;
        ReceiverName   = dto.ReceiverName   ?? string.Empty;
        DeliveredBy    = dto.DeliveredBy    ?? string.Empty;
        ReceivedBy     = dto.ReceivedBy     ?? string.Empty;
        VehiclePlate   = dto.VehiclePlate   ?? string.Empty;
        TrackingNumber = dto.TrackingNumber ?? string.Empty;
        Notes          = dto.Notes          ?? string.Empty;

        SelectedShipmentType  = dto.ShipmentTypeDisplay ?? "Evrak";
        SelectedStatus        = dto.StatusDisplay;
        _notificationStatus   = dto.NotificationStatus;

        await LoadLookupsAsync();

        SelectedCargoCompany = dto.CargoCompanyId.HasValue
            ? CargoCompanies.FirstOrDefault(x => x.Id == dto.CargoCompanyId.Value)
            : null;
        SelectedCompanyDirectory = dto.CompanyDirectoryId.HasValue
            ? CompanyDirectories.FirstOrDefault(x => x.Id == dto.CompanyDirectoryId.Value)
            : null;

        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(IsEditMode));
    }

    private void FillFromDirectory(CompanyDirectoryDto d)
    {
        // Otomatik doldurma: kullanıcı override edebilir
        if (string.IsNullOrWhiteSpace(ReceiverName))
            ReceiverName = d.CompanyName;
    }

    private async Task ExecuteSaveAsync()
    {
        ErrorMessage = null;

        var shipmentType = ParseShipmentType(SelectedShipmentType);
        var status       = ParseStatus(SelectedStatus);

        if (IsEditMode)
        {
            var req = new UpdateCargoShipmentRequest
            {
                Id                 = _editTargetId!.Value,
                ShipmentNumber     = NullIfEmpty(ShipmentNumber),
                Direction          = _direction,
                ShipmentDate       = ShipmentDate,
                ShipmentType       = shipmentType,
                CargoCompanyId     = SelectedCargoCompany?.Id,
                CompanyDirectoryId = SelectedCompanyDirectory?.Id,
                SenderName         = NullIfEmpty(SenderName),
                ReceiverName       = NullIfEmpty(ReceiverName),
                DeliveredBy        = NullIfEmpty(DeliveredBy),
                ReceivedBy         = NullIfEmpty(ReceivedBy),
                VehiclePlate       = NullIfEmpty(VehiclePlate),
                TrackingNumber     = NullIfEmpty(TrackingNumber),
                Status             = status,
                NotificationStatus = _notificationStatus,
                Notes              = NullIfEmpty(Notes),
                UpdatedByUserId    = _userContext.UserId
            };
            var result = await _updateHandler.HandleAsync(req);
            if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }
        }
        else
        {
            var req = new CreateCargoShipmentRequest
            {
                ShipmentNumber     = NullIfEmpty(ShipmentNumber),
                Direction          = _direction,
                ShipmentDate       = ShipmentDate,
                ShipmentType       = shipmentType,
                CargoCompanyId     = SelectedCargoCompany?.Id,
                CompanyDirectoryId = SelectedCompanyDirectory?.Id,
                SenderName         = NullIfEmpty(SenderName),
                ReceiverName       = NullIfEmpty(ReceiverName),
                DeliveredBy        = NullIfEmpty(DeliveredBy),
                ReceivedBy         = NullIfEmpty(ReceivedBy),
                VehiclePlate       = NullIfEmpty(VehiclePlate),
                TrackingNumber     = NullIfEmpty(TrackingNumber),
                Status             = status,
                Notes              = NullIfEmpty(Notes),
                CreatedByUserId    = _userContext.UserId
            };
            var result = await _createHandler.HandleAsync(req);
            if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }
        }

        SaveCompleted?.Invoke();
    }

    private static CargoShipmentType? ParseShipmentType(string display) => display switch
    {
        "Evrak"       => CargoShipmentType.Document,
        "Numune"      => CargoShipmentType.Sample,
        "Fatura"      => CargoShipmentType.Invoice,
        "Sözleşme"    => CargoShipmentType.Contract,
        "Yedek Parça" => CargoShipmentType.SparePart,
        "Diğer"       => CargoShipmentType.Other,
        _             => null
    };

    private static CargoShipmentStatus ParseStatus(string display) => display switch
    {
        "Hazırlandı"    => CargoShipmentStatus.Prepared,
        "Gönderildi"    => CargoShipmentStatus.Shipped,
        "Alındı"        => CargoShipmentStatus.Received,
        "Teslim Edildi" => CargoShipmentStatus.Delivered,
        "İptal"         => CargoShipmentStatus.Cancelled,
        _               => CargoShipmentStatus.Draft
    };

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
