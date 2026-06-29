using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Queries.GetCargoCompanyList;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Queries.GetCompanyDirectoryList;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.CreateCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.UpdateCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyAttentionContacts.EnsureCompanyAttentionContact;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyAttentionContacts.GetCompanyAttentionContacts;
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
    private readonly GetCompanyAttentionContactsHandler _attentionContactsHandler;
    private readonly EnsureCompanyAttentionContactHandler _ensureAttentionContactHandler;
    private readonly IUserContext _userContext;

    private Guid? _editTargetId;
    private CargoShipmentDirection _direction;
    private CargoShipmentStatus _currentEntityStatus = CargoShipmentStatus.Draft;
    // Kullanıcı "Firma Bilgilerini Yenile" bastıysa true; kayıtta snapshot request'e dahil edilir
    private bool _snapshotRefreshed;

    private DateTime _shipmentDate = DateTime.Today;
    private string _shipmentNumber = string.Empty;
    private string _senderName    = string.Empty;
    private string _receiverName  = string.Empty;
    private string _deliveredBy   = string.Empty;
    private string _receivedBy    = string.Empty;
    private string _vehiclePlate  = string.Empty;
    private string _trackingNumber = string.Empty;
    private string _trackingUrl    = string.Empty;
    private string _notes                 = string.Empty;
    private string _attentionContactInput = string.Empty;
    private CargoCompanyDto? _selectedCargoCompany;
    private CompanyDirectoryDto? _selectedCompanyDirectory;
    private string _selectedShipmentType        = "Evrak";
    private string _selectedStatus              = "Gönderime Hazır";
    private string _selectedNotificationStatus  = "Bildirilmedi";
    private string _selectedPriority            = "Normal";
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
    public string VehiclePlate   { get => _vehiclePlate;   set { _vehiclePlate  = (value ?? string.Empty).ToUpperInvariant(); OnPropertyChanged(); } }
    public string TrackingNumber
    {
        get => _trackingNumber;
        set
        {
            _trackingNumber = value;
            OnPropertyChanged();
            // TrackingUrl boşsa otomatik üret; kullanıcı manuel girmişse dokunma
            if (string.IsNullOrWhiteSpace(TrackingUrl))
                TryAutoFillTrackingUrl();
        }
    }
    public string TrackingUrl
    {
        get => _trackingUrl;
        set { _trackingUrl = value; OnPropertyChanged(); }
    }
    public string Notes  { get => _notes; set { _notes = value; OnPropertyChanged(); } }

    /// <summary>Kargo için dikkatine kişisi — firma listesinden seçilir veya serbest girilir.</summary>
    public string AttentionContactInput
    {
        get => _attentionContactInput;
        set { _attentionContactInput = value; OnPropertyChanged(); }
    }

    /// <summary>Seçili firmaya ait geçmiş dikkatine kişileri (son kullanılan önce).</summary>
    public ObservableCollection<string> AttentionContacts { get; } = [];

    public CargoCompanyDto? SelectedCargoCompany
    {
        get => _selectedCargoCompany;
        set
        {
            _selectedCargoCompany = value;
            OnPropertyChanged();
            if (string.IsNullOrWhiteSpace(TrackingUrl))
                TryAutoFillTrackingUrl();
        }
    }

    public CompanyDirectoryDto? SelectedCompanyDirectory
    {
        get => _selectedCompanyDirectory;
        set
        {
            _selectedCompanyDirectory = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasDirectoryDetails));
            OnPropertyChanged(nameof(DirectoryFirma));
            OnPropertyChanged(nameof(DirectoryContact));
            OnPropertyChanged(nameof(DirectoryContactDisplay));
            OnPropertyChanged(nameof(DirectoryAddress));
            OnPropertyChanged(nameof(DirectoryPhone));
            OnPropertyChanged(nameof(DirectoryEmail));
            OnPropertyChanged(nameof(HasDirectoryContact));
            OnPropertyChanged(nameof(HasDirectoryAddress));
            OnPropertyChanged(nameof(HasDirectoryPhone));
            OnPropertyChanged(nameof(HasDirectoryEmail));
            OnPropertyChanged(nameof(HasRefreshableSnapshot));
            // Giden kargoda firma seçilince alıcı adı otomatik dolar
            if (value is not null && _direction == CargoShipmentDirection.Outgoing)
                FillFromDirectory(value);
            // Firma değişince dikkatine listesi güncellenir; varsayılan = firmanın mevcut AttentionTo
            _ = LoadAttentionContactsAsync(value?.Id, defaultInput: value?.AttentionTo);
        }
    }

    /// <summary>Firma seçildiğinde kart paneli gösterilir.</summary>
    public bool HasDirectoryDetails => _selectedCompanyDirectory is not null;

    /// <summary>Düzenleme modunda ve firma seçiliyse "Firma Bilgilerini Yenile" butonu görünür.</summary>
    public bool HasRefreshableSnapshot => IsEditMode && _selectedCompanyDirectory is not null;

    // ── Firma Kart Alanları — her biri INPC tetikler ─────────────────────
    public string? DirectoryFirma   => _selectedCompanyDirectory?.CompanyName;
    public string? DirectoryContact => _selectedCompanyDirectory?.AttentionTo;

    /// <summary>"İlgili: Abuzer Bey Dikkatine" veya boş (gizlenecek satır).</summary>
    public string DirectoryContactDisplay => AttentionHelper.FormatAttentionDisplay(_selectedCompanyDirectory?.AttentionTo);
    public string? DirectoryAddress => BuildDirectoryAddress(_selectedCompanyDirectory);
    public string? DirectoryPhone   => _selectedCompanyDirectory?.Phone;
    public string? DirectoryEmail   => _selectedCompanyDirectory?.Email;

    public bool HasDirectoryContact => !string.IsNullOrWhiteSpace(DirectoryContact);
    public bool HasDirectoryAddress => !string.IsNullOrWhiteSpace(DirectoryAddress);
    public bool HasDirectoryPhone   => !string.IsNullOrWhiteSpace(DirectoryPhone);
    public bool HasDirectoryEmail   => !string.IsNullOrWhiteSpace(DirectoryEmail);

    private static string? BuildDirectoryAddress(CompanyDirectoryDto? d)
    {
        if (d is null) return null;
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(d.AddressLine)) parts.Add(d.AddressLine);
        var loc = string.Join(" / ", new[] { d.District, d.City }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        if (!string.IsNullOrWhiteSpace(loc)) parts.Add(loc);
        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    public string SelectedShipmentType
    {
        get => _selectedShipmentType;
        set { _selectedShipmentType = value; OnPropertyChanged(); }
    }

    public string SelectedPriority
    {
        get => _selectedPriority;
        set { _selectedPriority = value; OnPropertyChanged(); }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set { _selectedStatus = value; OnPropertyChanged(); }
    }

    public string SelectedNotificationStatus
    {
        get => _selectedNotificationStatus;
        set { _selectedNotificationStatus = value; OnPropertyChanged(); }
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

    public IReadOnlyList<string> PriorityOptions { get; } =
        ["Normal", "Orta", "Acil", "Çok Acil"];

    /// <summary>
    /// Yeni kayıtta yöne göre uygun durumlar sunulur; düzenlemede sadece geçerli geçişler listelenir.
    /// Gelen kargoda Hazırlandı ve Gönderildi gösterilmez.
    /// </summary>
    public IReadOnlyList<string> AllowedStatusOptions
    {
        get
        {
            if (!IsEditMode)
                return _direction == CargoShipmentDirection.Incoming
                    ? _incomingStatusLabels
                    : _allStatusLabels;

            return CargoStatusTransitions
                .GetAllowedNext(_currentEntityStatus, _direction)
                .Select(DisplayStatus)
                .ToList();
        }
    }

    public IReadOnlyList<string> NotificationStatusOptions { get; } =
        ["Bildirilmedi", "WhatsApp Hazır", "Mail Hazır", "Bildirildi"];

    // Giden kargo yeni kayıt durum listesi
    private static readonly IReadOnlyList<string> _allStatusLabels =
        ["Gönderime Hazır", "Kargoya Teslim Edildi", "Gönderildi", "Teslim Edildi"];

    // Gelen kargo yeni kayıt durum listesi
    private static readonly IReadOnlyList<string> _incomingStatusLabels =
        ["Bekleniyor", "Teslim Alındı", "Personele Teslim Edildi"];

    public Action? SaveCompleted { get; set; }
    public ICommand SaveCommand { get; }

    public CargoShipmentEditViewModel(
        CreateCargoShipmentHandler createHandler,
        UpdateCargoShipmentHandler updateHandler,
        GetCargoCompanyListHandler cargoCompanyListHandler,
        GetCompanyDirectoryListHandler directoryListHandler,
        GetCompanyAttentionContactsHandler attentionContactsHandler,
        EnsureCompanyAttentionContactHandler ensureAttentionContactHandler,
        IUserContext userContext)
    {
        _createHandler                = createHandler;
        _updateHandler                = updateHandler;
        _cargoCompanyListHandler      = cargoCompanyListHandler;
        _directoryListHandler         = directoryListHandler;
        _attentionContactsHandler     = attentionContactsHandler;
        _ensureAttentionContactHandler = ensureAttentionContactHandler;
        _userContext                  = userContext;
        SaveCommand                   = new RelayCommand(async () => await ExecuteSaveAsync());
    }

    public void SetDirection(CargoShipmentDirection direction)
    {
        _direction = direction;
        // Yeni kayıtta yöne göre varsayılan durum
        _selectedStatus = direction == CargoShipmentDirection.Incoming ? "Teslim Alındı" : "Gönderime Hazır";
        OnPropertyChanged(nameof(SelectedStatus));
        OnPropertyChanged(nameof(AllowedStatusOptions));
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
        IsEditMode             = true;
        _editTargetId          = dto.Id;
        _direction             = dto.Direction;
        _currentEntityStatus   = dto.Status;
        ShipmentDate           = dto.ShipmentDate;
        ShipmentNumber         = dto.ShipmentNumber ?? string.Empty;
        SenderName             = dto.SenderName     ?? string.Empty;
        ReceiverName           = dto.ReceiverName   ?? string.Empty;
        DeliveredBy            = dto.DeliveredBy    ?? string.Empty;
        ReceivedBy             = dto.ReceivedBy     ?? string.Empty;
        VehiclePlate           = dto.VehiclePlate   ?? string.Empty;
        _trackingNumber        = dto.TrackingNumber ?? string.Empty; // backing field: TrackingUrl auto-fill tetiklenmesin
        TrackingUrl            = dto.TrackingUrl    ?? string.Empty;
        OnPropertyChanged(nameof(TrackingNumber));
        Notes                  = dto.Notes          ?? string.Empty;
        SelectedShipmentType        = dto.ShipmentTypeDisplay ?? "Evrak";
        SelectedPriority            = dto.PriorityDisplay;
        SelectedStatus              = dto.StatusDisplay;
        SelectedNotificationStatus  = DisplayNotificationStatus(dto.NotificationStatus);

        OnPropertyChanged(nameof(AllowedStatusOptions));

        await LoadLookupsAsync();

        SelectedCargoCompany = dto.CargoCompanyId.HasValue
            ? CargoCompanies.FirstOrDefault(x => x.Id == dto.CargoCompanyId.Value)
            : null;
        SelectedCompanyDirectory = dto.CompanyDirectoryId.HasValue
            ? CompanyDirectories.FirstOrDefault(x => x.Id == dto.CompanyDirectoryId.Value)
            : null;

        // Dikkatine: mevcut kargo kaydındaki snapshot değerini yükle (firma AttentionTo'yu override edebilir)
        if (dto.CompanyDirectoryId.HasValue)
            await LoadAttentionContactsAsync(dto.CompanyDirectoryId, defaultInput: dto.ReceiverAttentionSnapshot);
        AttentionContactInput = dto.ReceiverAttentionSnapshot ?? string.Empty;

        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(HasRefreshableSnapshot));
    }

    /// <summary>
    /// Kopyala: ID/ShipmentNumber/audit/TrackingNumber/TrackingUrl/Status/NotificationStatus sıfırlanır,
    /// geri kalan operasyonel alanlar kaynak kayıttan doldurulur.
    /// </summary>
    public async Task InitializeForCopyAsync(CargoShipmentDto source)
    {
        IsEditMode  = false;
        _direction  = source.Direction;
        ShipmentDate       = DateTime.Today;
        ShipmentNumber     = string.Empty; // handler yeni numara üretir
        SenderName         = source.SenderName   ?? string.Empty;
        ReceiverName       = source.ReceiverName ?? string.Empty;
        DeliveredBy        = string.Empty;
        ReceivedBy         = string.Empty;
        VehiclePlate       = source.VehiclePlate ?? string.Empty;
        _trackingNumber    = string.Empty; // TrackingUrl auto-fill tetiklenmesin
        TrackingUrl        = string.Empty;
        OnPropertyChanged(nameof(TrackingNumber));
        Notes              = source.Notes ?? string.Empty;
        SelectedShipmentType       = source.ShipmentTypeDisplay ?? "Evrak";
        SelectedPriority           = source.PriorityDisplay;
        // Kopyalama: yöne göre varsayılan durum
        SelectedStatus             = source.Direction == CargoShipmentDirection.Incoming ? "Teslim Alındı" : "Gönderime Hazır";
        SelectedNotificationStatus = "Bildirilmedi";

        OnPropertyChanged(nameof(AllowedStatusOptions));
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(IsEditMode));

        await LoadLookupsAsync();

        SelectedCargoCompany = source.CargoCompanyId.HasValue
            ? CargoCompanies.FirstOrDefault(x => x.Id == source.CargoCompanyId.Value)
            : null;
        // CompanyDirectory seçimi: ID ile bul, setter snapshot bildirimlerini tetikler
        SelectedCompanyDirectory = source.CompanyDirectoryId.HasValue
            ? CompanyDirectories.FirstOrDefault(x => x.Id == source.CompanyDirectoryId.Value)
            : null;
    }

    /// <summary>
    /// Seçili firma rehberi verilerinden alıcı snapshot'ını tazeler.
    /// Yalnızca kullanıcı bilinçli "Firma Bilgilerini Yenile" butonuna bastığında çağrılır.
    /// DB'ye yazılmaz; kaydetme akışı snapshot'ı request'e dahil eder.
    /// </summary>
    public void RefreshSnapshotFromDirectory()
    {
        if (_selectedCompanyDirectory is null || !IsEditMode) return;
        _snapshotRefreshed = true;
    }

    /// <summary>
    /// Firma için geçmiş dikkatine kişilerini yükler.
    /// Setter'dan fire-and-forget olarak çağrılır; defaultInput verilmişse AttentionContactInput de güncellenir.
    /// </summary>
    private async Task LoadAttentionContactsAsync(Guid? companyDirectoryId, string? defaultInput = null)
    {
        AttentionContacts.Clear();
        if (companyDirectoryId is null) return;

        try
        {
            var contacts = await _attentionContactsHandler.HandleAsync(
                new GetCompanyAttentionContactsQuery(companyDirectoryId.Value));

            foreach (var c in contacts)
                AttentionContacts.Add(c.Name);
        }
        catch
        {
            // Dikkatine listesi yüklenemedi — form çalışmaya devam eder, özellik sessizce devre dışı kalır
        }

        if (defaultInput is not null)
            AttentionContactInput = defaultInput;
    }

    /// <summary>
    /// Kullanıcı "+" butonuna bastığında mevcut girişi firmanın dikkatine listesine ekler.
    /// </summary>
    public async Task AddAttentionContactAsync(string name)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrEmpty(trimmed) || _selectedCompanyDirectory is null) return;

        // Zaten listede varsa sadece input'u güncelle
        if (!AttentionContacts.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
        {
            await _ensureAttentionContactHandler.HandleAsync(
                new EnsureCompanyAttentionContactRequest(
                    _selectedCompanyDirectory.Id, trimmed, _userContext.UserId));

            // Listeyi yenile
            await LoadAttentionContactsAsync(_selectedCompanyDirectory.Id, defaultInput: trimmed);
        }
        AttentionContactInput = trimmed;
    }

    private void FillFromDirectory(CompanyDirectoryDto d)
    {
        // Otomatik doldurma: kullanıcı override edebilir
        if (string.IsNullOrWhiteSpace(ReceiverName))
            ReceiverName = d.CompanyName;
    }

    private void TryAutoFillTrackingUrl()
    {
        if (_selectedCargoCompany is null) return;
        if (string.IsNullOrWhiteSpace(_selectedCargoCompany.TrackingUrlTemplate)) return;
        if (string.IsNullOrWhiteSpace(_trackingNumber)) return;
        TrackingUrl = string.Format(_selectedCargoCompany.TrackingUrlTemplate, _trackingNumber.Trim());
    }

    private async Task ExecuteSaveAsync()
    {
        ErrorMessage = null;

        var shipmentType       = ParseShipmentType(SelectedShipmentType);
        var priority           = ParsePriority(SelectedPriority);
        var status             = ParseStatus(SelectedStatus);
        var notificationStatus = ParseNotificationStatus(SelectedNotificationStatus);

        if (IsEditMode)
        {
            var req = new UpdateCargoShipmentRequest
            {
                Id                 = _editTargetId!.Value,
                ShipmentNumber     = NullIfEmpty(ShipmentNumber),
                Direction          = _direction,
                ShipmentDate       = ShipmentDate,
                ShipmentType       = shipmentType,
                Priority           = priority,
                CargoCompanyId     = SelectedCargoCompany?.Id,
                CompanyDirectoryId = SelectedCompanyDirectory?.Id,
                SenderName         = NullIfEmpty(SenderName),
                ReceiverName       = NullIfEmpty(ReceiverName),
                DeliveredBy        = NullIfEmpty(DeliveredBy),
                ReceivedBy         = NullIfEmpty(ReceivedBy),
                VehiclePlate       = NullIfEmpty(VehiclePlate),
                TrackingNumber     = NullIfEmpty(TrackingNumber),
                TrackingUrl        = NullIfEmpty(TrackingUrl),
                Status             = status,
                NotificationStatus = notificationStatus,
                Notes              = NullIfEmpty(Notes),
                UpdatedByUserId    = _userContext.UserId,

                // Kullanıcı "Firma Bilgilerini Yenile" bastıysa seçili firma verilerinden snapshot güncellenir
                UpdateSnapshot      = _snapshotRefreshed,
                SnapshotCompanyName = _snapshotRefreshed ? _selectedCompanyDirectory?.CompanyName : null,
                SnapshotAddress     = _snapshotRefreshed ? _selectedCompanyDirectory?.AddressLine  : null,
                SnapshotAttention   = _snapshotRefreshed ? NullIfEmpty(AttentionContactInput) : null,
                SnapshotCity        = _snapshotRefreshed ? _selectedCompanyDirectory?.City         : null,
                SnapshotDistrict    = _snapshotRefreshed ? _selectedCompanyDirectory?.District     : null,
                SnapshotPhone       = _snapshotRefreshed ? _selectedCompanyDirectory?.Phone        : null,
                SnapshotEmail       = _snapshotRefreshed ? _selectedCompanyDirectory?.Email        : null,
            };
            var result = await _updateHandler.HandleAsync(req);
            if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }
        }
        else
        {
            var dir = _selectedCompanyDirectory;
            var req = new CreateCargoShipmentRequest
            {
                ShipmentNumber     = NullIfEmpty(ShipmentNumber),
                Direction          = _direction,
                ShipmentDate       = ShipmentDate,
                ShipmentType       = shipmentType,
                Priority           = priority,
                CargoCompanyId     = SelectedCargoCompany?.Id,
                CompanyDirectoryId = dir?.Id,

                // Firma seçilmişse snapshot alınır — adres ileriden değişse bile kargo kaydı korunur
                ReceiverCompanyNameSnapshot = dir?.CompanyName,
                ReceiverAddressSnapshot     = dir?.AddressLine,
                // Kullanıcının seçtiği/yazdığı dikkatine kişisi snapshot'a yazılır
                ReceiverAttentionSnapshot   = NullIfEmpty(AttentionContactInput),
                ReceiverCitySnapshot        = dir?.City,
                ReceiverDistrictSnapshot    = dir?.District,
                ReceiverPhoneSnapshot       = dir?.Phone,
                ReceiverEmailSnapshot       = dir?.Email,

                SenderName         = NullIfEmpty(SenderName),
                ReceiverName       = NullIfEmpty(ReceiverName),
                DeliveredBy        = NullIfEmpty(DeliveredBy),
                ReceivedBy         = NullIfEmpty(ReceivedBy),
                VehiclePlate       = NullIfEmpty(VehiclePlate),
                TrackingNumber     = NullIfEmpty(TrackingNumber),
                TrackingUrl        = NullIfEmpty(TrackingUrl),
                Status             = status,
                Notes              = NullIfEmpty(Notes),
                CreatedByUserId    = _userContext.UserId
            };
            var result = await _createHandler.HandleAsync(req);
            if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }
        }

        // Dikkatine kişisini firma listesinde tazele (hata oluşursa kargo kaydı etkilenmez)
        if (_selectedCompanyDirectory is not null && !string.IsNullOrWhiteSpace(AttentionContactInput))
        {
            try
            {
                await _ensureAttentionContactHandler.HandleAsync(
                    new EnsureCompanyAttentionContactRequest(
                        _selectedCompanyDirectory.Id, AttentionContactInput.Trim(), _userContext.UserId));
            }
            catch
            {
                // Dikkatine güncelleme başarısız — kargo başarıyla kaydedildi, non-fatal
            }
        }

        SaveCompleted?.Invoke();
    }

    private static CargoShipmentPriority ParsePriority(string display) => display switch
    {
        "Orta"     => CargoShipmentPriority.Medium,
        "Acil"     => CargoShipmentPriority.Urgent,
        "Çok Acil" => CargoShipmentPriority.Critical,
        _          => CargoShipmentPriority.Normal
    };

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
        "Gönderime Hazır"         => CargoShipmentStatus.Prepared,
        "Hazırlandı"              => CargoShipmentStatus.Prepared,           // eski kayıt uyumluluğu
        "Kargoya Teslim Edildi"   => CargoShipmentStatus.HandedToCargo,
        "Gönderildi"              => CargoShipmentStatus.Shipped,
        "Bekleniyor"              => CargoShipmentStatus.Waiting,
        "Teslim Alındı"           => CargoShipmentStatus.Received,
        "Alındı"                  => CargoShipmentStatus.Received,           // eski kayıt uyumluluğu
        "Personele Teslim Edildi" => CargoShipmentStatus.PersonnelDelivered,
        "Teslim Edildi"           => CargoShipmentStatus.Delivered,
        "İptal"                   => CargoShipmentStatus.Cancelled,
        _                         => CargoShipmentStatus.Draft               // "Taslak" eski kayıtlar
    };

    private static CargoNotificationStatus ParseNotificationStatus(string display) => display switch
    {
        "WhatsApp Hazır" => CargoNotificationStatus.WhatsAppPrepared,
        "Mail Hazır"     => CargoNotificationStatus.MailPrepared,
        "Bildirildi"     => CargoNotificationStatus.Notified,
        _                => CargoNotificationStatus.NotNotified
    };

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

    private static string DisplayNotificationStatus(CargoNotificationStatus ns) => ns switch
    {
        CargoNotificationStatus.WhatsAppPrepared => "WhatsApp Hazır",
        CargoNotificationStatus.MailPrepared     => "Mail Hazır",
        CargoNotificationStatus.Notified         => "Bildirildi",
        _                                        => "Bildirilmedi"
    };

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
