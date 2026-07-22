using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.CreateWhatsAppContact;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.UpdateWhatsAppContact;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.WhatsApp;

public class WhatsAppContactEditViewModel : INotifyPropertyChanged
{
    private readonly CreateWhatsAppContactHandler _createHandler;
    private readonly UpdateWhatsAppContactHandler _updateHandler;

    private Guid? _editTargetId;
    private string _fullName    = string.Empty;
    private string _phone       = string.Empty;
    private string _company     = string.Empty;
    private string _description = string.Empty;
    private bool _isActive = true;
    private string? _errorMessage;

    public bool IsEditMode { get; private set; }
    public string WindowTitle => IsEditMode ? "Rehber Kişisi Düzenle" : "Yeni Rehber Kişisi";

    public string FullName    { get => _fullName;    set { _fullName    = value; OnPropertyChanged(); } }
    public string Phone       { get => _phone;       set { _phone       = value; OnPropertyChanged(); } }
    public string Company     { get => _company;     set { _company     = value; OnPropertyChanged(); } }
    public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }
    public bool   IsActive    { get => _isActive;    set { _isActive    = value; OnPropertyChanged(); } }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    /// <summary>Kaydetme sonrası oluşan/güncellenen kişi — hızlı ekleme akışında otomatik seçim için.</summary>
    public WhatsAppContactDto? SavedContact { get; private set; }

    public Action? SaveCompleted { get; set; }
    public ICommand SaveCommand { get; }

    public WhatsAppContactEditViewModel(
        CreateWhatsAppContactHandler createHandler,
        UpdateWhatsAppContactHandler updateHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        SaveCommand    = new RelayCommand(async () => await ExecuteSaveAsync());
    }

    public void InitializeForEdit(WhatsAppContactDto dto)
    {
        IsEditMode    = true;
        _editTargetId = dto.Id;
        FullName      = dto.FullName;
        Phone         = dto.Phone;
        Company       = dto.Company ?? string.Empty;
        Description   = dto.Description ?? string.Empty;
        IsActive      = dto.IsActive;
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(IsEditMode));
    }

    private async Task ExecuteSaveAsync()
    {
        ErrorMessage = null;

        if (IsEditMode)
        {
            var result = await _updateHandler.HandleAsync(new UpdateWhatsAppContactRequest
            {
                Id          = _editTargetId!.Value,
                FullName    = FullName,
                Phone       = Phone,
                Company     = NullIfEmpty(Company),
                Description = NullIfEmpty(Description),
                IsActive    = IsActive
            });
            if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }
        }
        else
        {
            var result = await _createHandler.HandleAsync(new CreateWhatsAppContactRequest
            {
                FullName    = FullName,
                Phone       = Phone,
                Company     = NullIfEmpty(Company),
                Description = NullIfEmpty(Description)
            });
            if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }
            SavedContact = result.Data;
        }

        SaveCompleted?.Invoke();
    }

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
