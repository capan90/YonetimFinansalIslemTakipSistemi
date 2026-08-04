using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Serilog;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Queries.GetCargoCompanyList;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Queries.GetCompanyDirectoryList;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.CreateCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.UpdateCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoPartySuggestions;
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
    private readonly GetCargoPartySuggestionsHandler _partySuggestionsHandler;
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
    public string ShipmentNumber  { get => _shipmentNumber; set { _shipmentNumber = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShipmentNumberDisplay)); } }

    /// <summary>
    /// Salt okunur numara alanı gösterimi. Yeni kayıtta numara form açılırken rezerve
    /// edilmez; yalnızca UI placeholder metni gösterilir (entity/DB'ye yazılmaz).
    /// Düzenlemede gerçek numara görünür.
    /// </summary>
    public string ShipmentNumberDisplay => IsEditMode
        ? ShipmentNumber
        : "Kaydedildiğinde otomatik oluşturulacak";

    /// <summary>Create başarılı olunca handler'ın ürettiği numara — başarı mesajında gösterilir.</summary>
    public string? SavedShipmentNumber { get; private set; }
    // ?? string.Empty: düzenlenebilir ComboBox'ın Text binding'i seçim temizlendiğinde
    // null gönderebilir (TextBox'ta bu durum oluşmuyordu)
    public string SenderName      { get => _senderName;     set { _senderName    = value ?? string.Empty; OnPropertyChanged(); } }
    public string ReceiverName    { get => _receiverName;   set { _receiverName  = value ?? string.Empty; OnPropertyChanged(); } }
    public string DeliveredBy     { get => _deliveredBy;    set { _deliveredBy   = value ?? string.Empty; OnPropertyChanged(); } }
    public string ReceivedBy      { get => _receivedBy;     set { _receivedBy    = value ?? string.Empty; OnPropertyChanged(); } }
    // Harf dönüşümü UI'da zorlanmaz; kayıt öncesi kullanıcı tercihine göre merkezi serviste yapılır
    public string VehiclePlate   { get => _vehiclePlate;   set { _vehiclePlate  = value ?? string.Empty; OnPropertyChanged(); } }
    public string TrackingNumber
    {
        get => _trackingNumber;
        set { _trackingNumber = value; OnPropertyChanged(); }
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

    // Firma seçimi programatik yapıldığında (Initialize/Copy) setter'daki fire-and-forget yükleme bastırılır.
    private bool _suppressAttentionReload;

    // Oturum boyunca tek DbContext paylaşılır; eşzamanlı sorgu
    // "A second operation was started on this context instance" hatasına yol açar.
    // Kuyruk mantığı ReloadCoordinator'da; en son argümanlar _attentionArgs'ta tutulur
    // (parametreli senaryo: delege bu alanı okur → kuyruğa alınan tekrar en son firmayı yükler).
    private readonly ReloadCoordinator _attentionCoordinator = new();
    private (Guid? CompanyDirectoryId, string? DefaultInput) _attentionArgs;

    public CargoCompanyDto? SelectedCargoCompany
    {
        get => _selectedCargoCompany;
        set
        {
            // "— Seçim yok —" satırı seçilirse kayıt boşaltılır; OnPropertyChanged
            // null'ı ComboBox'a geri yazar ve alan boş görünür
            _selectedCargoCompany = ReferenceEquals(value, _noCargoCompany) ? null : value;
            OnPropertyChanged();
            // Portal/takip bağlantısı seçili firmanın kaydından gelir — tek kaynak
            // CargoCompany.PortalUrl; firma adına if/else yazılmaz
            OnPropertyChanged(nameof(SelectedCompanyPortalUrl));
            OnPropertyChanged(nameof(HasPortalUrl));
        }
    }

    /// <summary>Seçili kargo firmasının portal/takip bağlantısı; UI'da salt okunur gösterilir.</summary>
    public string? SelectedCompanyPortalUrl => _selectedCargoCompany?.PortalUrl;

    public bool HasPortalUrl => !string.IsNullOrWhiteSpace(SelectedCompanyPortalUrl);

    public CompanyDirectoryDto? SelectedCompanyDirectory
    {
        get => _selectedCompanyDirectory;
        set
        {
            // "— Seçim yok —" satırı seçilirse firma bağlantısı kaldırılır;
            // aşağıdaki otomatik doldurma/dikkatine yüklemesi de null ile çalışır
            value = ReferenceEquals(value, _noCompanyDirectory) ? null : value;
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
            // (metot kendi içinde hatayı yutar — form çalışmaya devam eder; Forget yine de güvence sağlar)
            // Düzenleme/kopyalama başlatılırken çağıran kendi yüklemesini await ettiği için
            // buradaki fire-and-forget bastırılır: aksi halde aynı DbContext üzerinde iki sorgu
            // yarışır ve dikkatine listesi sessizce boş kalır.
            if (!_suppressAttentionReload)
                LoadAttentionContactsAsync(value?.Id, defaultInput: value?.AttentionTo).Forget();
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

    // ── Gönderi / Teslim isim önerileri ───────────────────────────────────
    // Geçmiş kargo kayıtlarından türetilir; kullanıcı listeden seçebilir veya
    // yeni isim yazabilir (ComboBox IsEditable=True).
    public ObservableCollection<string> SenderNameSuggestions   { get; } = [];
    public ObservableCollection<string> ReceiverNameSuggestions { get; } = [];
    public ObservableCollection<string> DeliveredBySuggestions  { get; } = [];
    public ObservableCollection<string> ReceivedBySuggestions   { get; } = [];

    /// <summary>
    /// Opsiyonel ComboBox'larda seçimi geri temizlemek için listenin başına eklenen satır.
    /// Seçildiğinde ilgili property null'a döner (DB kolonları zaten nullable).
    /// </summary>
    public const string NoSelectionLabel = "— Seçim yok —";

    // Sentinel örnekler: liste tipini bozmadan "boş" satırı temsil ederler.
    // Id = Guid.Empty olduğundan gerçek kayıtlarla asla eşleşmezler.
    private static readonly CargoCompanyDto _noCargoCompany =
        new() { Id = Guid.Empty, Name = NoSelectionLabel };

    private static readonly CompanyDirectoryDto _noCompanyDirectory =
        new() { Id = Guid.Empty, CompanyName = NoSelectionLabel };

    public IReadOnlyList<string> ShipmentTypeOptions { get; } =
        [NoSelectionLabel, "Evrak", "Numune", "Fatura", "Sözleşme", "Yedek Parça", "Diğer"];

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
        GetCargoPartySuggestionsHandler partySuggestionsHandler,
        IUserContext userContext)
    {
        _createHandler                = createHandler;
        _updateHandler                = updateHandler;
        _cargoCompanyListHandler      = cargoCompanyListHandler;
        _directoryListHandler         = directoryListHandler;
        _attentionContactsHandler     = attentionContactsHandler;
        _ensureAttentionContactHandler = ensureAttentionContactHandler;
        _partySuggestionsHandler      = partySuggestionsHandler;
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
        // Boş seçim satırı en üstte: kullanıcı yanlış seçtiği firmayı geri temizleyebilsin
        CargoCompanies.Add(_noCargoCompany);
        foreach (var c in companies)
            CargoCompanies.Add(c);

        var dirs = await _directoryListHandler.HandleAsync(
            new GetCompanyDirectoryListQuery { IsActive = true });
        CompanyDirectories.Clear();
        CompanyDirectories.Add(_noCompanyDirectory);
        foreach (var d in dirs)
            CompanyDirectories.Add(d);

        await LoadPartySuggestionsAsync();
    }

    /// <summary>
    /// Gönderen/Alıcı/Teslim Eden/Teslim Alan önerilerini geçmiş kayıtlardan yükler.
    /// Öneri listesi ikincil bir kolaylıktır — hata olursa form normal çalışmaya devam eder.
    /// </summary>
    private async Task LoadPartySuggestionsAsync()
    {
        try
        {
            var s = await _partySuggestionsHandler.HandleAsync(
                new GetCargoPartySuggestionsQuery(_direction));

            Fill(SenderNameSuggestions,   s.SenderNames);
            Fill(ReceiverNameSuggestions, s.ReceiverNames);
            Fill(DeliveredBySuggestions,  s.DeliveredBy);
            Fill(ReceivedBySuggestions,   s.ReceivedBy);
        }
        catch (Exception ex)
        {
            // Sessizce yutulmaz: aksi halde öneriler aylarca boş kalır ve kimse fark etmez
            Log.Warning(ex, "Gönderi/teslim isim önerileri yüklenemedi (Direction={Direction})", _direction);
        }

        static void Fill(ObservableCollection<string> target, IReadOnlyList<string> source)
        {
            target.Clear();
            foreach (var v in source) target.Add(v);
        }
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
        TrackingNumber         = dto.TrackingNumber ?? string.Empty;
        Notes                  = dto.Notes          ?? string.Empty;
        // Kargo türü opsiyonel: kayıtta boşsa "Evrak" gibi görünmemeli
        SelectedShipmentType        = dto.ShipmentTypeDisplay ?? NoSelectionLabel;
        SelectedPriority            = dto.PriorityDisplay;
        SelectedStatus              = dto.StatusDisplay;
        SelectedNotificationStatus  = DisplayNotificationStatus(dto.NotificationStatus);

        OnPropertyChanged(nameof(AllowedStatusOptions));

        await LoadLookupsAsync();

        SelectedCargoCompany = dto.CargoCompanyId.HasValue
            ? CargoCompanies.FirstOrDefault(x => x.Id == dto.CargoCompanyId.Value)
            : null;
        // Yükleme aşağıda snapshot ile açıkça yapılır — setter'ın kendi yüklemesi bastırılır
        _suppressAttentionReload = true;
        try
        {
            SelectedCompanyDirectory = dto.CompanyDirectoryId.HasValue
                ? CompanyDirectories.FirstOrDefault(x => x.Id == dto.CompanyDirectoryId.Value)
                : null;
        }
        finally
        {
            _suppressAttentionReload = false;
        }

        // Dikkatine: mevcut kargo kaydındaki snapshot değerini yükle (firma AttentionTo'yu override edebilir)
        if (dto.CompanyDirectoryId.HasValue)
            await LoadAttentionContactsAsync(dto.CompanyDirectoryId, defaultInput: dto.ReceiverAttentionSnapshot);
        else
            AttentionContacts.Clear();
        AttentionContactInput = dto.ReceiverAttentionSnapshot ?? string.Empty;

        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(ShipmentNumberDisplay));
        OnPropertyChanged(nameof(HasRefreshableSnapshot));
    }

    /// <summary>
    /// Kopyala: ID/ShipmentNumber/audit/TrackingNumber/Status/NotificationStatus sıfırlanır,
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
        TrackingNumber     = string.Empty;
        Notes              = source.Notes ?? string.Empty;
        SelectedShipmentType       = source.ShipmentTypeDisplay ?? NoSelectionLabel;
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
    /// Yükleme sürerken gelen istek kuyruğa alınır (paylaşılan DbContext'te eşzamanlı sorgu çalıştırılmaz);
    /// yalnızca en son istek uygulanır.
    /// </summary>
    private Task LoadAttentionContactsAsync(Guid? companyDirectoryId, string? defaultInput = null)
    {
        // En son argümanlar saklanır; delege bunları okuduğundan kuyruğa alınan tekrar
        // (hızlı firma değişiminde) her zaman EN SON seçili firmayı yükler.
        _attentionArgs = (companyDirectoryId, defaultInput);
        return _attentionCoordinator.RunAsync(
            () => LoadAttentionContactsCoreAsync(_attentionArgs.CompanyDirectoryId, _attentionArgs.DefaultInput));
    }

    private async Task LoadAttentionContactsCoreAsync(Guid? companyDirectoryId, string? defaultInput)
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
        catch (Exception ex)
        {
            // Dikkatine listesi yüklenemedi — form çalışmaya devam eder, özellik devre dışı kalır.
            // Sessizce yutulmaz: aksi halde (ör. DbContext eşzamanlılığı) sorun aylarca fark edilmez.
            Log.Warning(ex, "Dikkatine listesi yüklenemedi (CompanyDirectoryId={CompanyDirectoryId})", companyDirectoryId);
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
                // ShipmentNumber gönderilmez — otomatik numara değiştirilemez
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
                // ShipmentNumber gönderilmez — handler atomik sayaçtan üretir (GLN/GDN)
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
                Status             = status,
                Notes              = NullIfEmpty(Notes),
                CreatedByUserId    = _userContext.UserId
            };
            var result = await _createHandler.HandleAsync(req);
            if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }
            SavedShipmentNumber = result.Data?.ShipmentNumber;
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
            catch (Exception ex)
            {
                // Dikkatine güncelleme başarısız — kargo başarıyla kaydedildi, non-fatal; yine de iz bırak
                Log.Warning(ex, "Dikkatine kişisi tazelenemedi (CompanyDirectoryId={CompanyDirectoryId})", _selectedCompanyDirectory.Id);
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
