using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts;
using YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts.CreateMailContact;
using YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts.UpdateMailContact;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Mail;

// WhatsAppContactEditViewModel ile aynı desen; farkı "Varsayılan CC" alanı.
public class MailContactEditViewModel : INotifyPropertyChanged
{
    private readonly CreateMailContactHandler _createHandler;
    private readonly UpdateMailContactHandler _updateHandler;

    private Guid?  _editTargetId;
    private string _fullName    = string.Empty;
    private string _email       = string.Empty;
    private string _company     = string.Empty;
    private string _description = string.Empty;
    private bool   _isDefaultCc;
    private bool   _isActive = true;
    private string? _errorMessage;

    public bool IsEditMode { get; private set; }
    public string WindowTitle => IsEditMode ? "Mail Kişisi Düzenle" : "Yeni Mail Kişisi";

    public string FullName    { get => _fullName;    set { _fullName    = value; OnPropertyChanged(); } }
    public string Email       { get => _email;       set { _email       = value; OnPropertyChanged(); } }
    public string Company     { get => _company;     set { _company     = value; OnPropertyChanged(); } }
    public string Description { get => _description; set { _description = value; OnPropertyChanged(); } }
    public bool   IsActive    { get => _isActive;    set { _isActive    = value; OnPropertyChanged(); } }

    /// <summary>İş kuralı: işaretliyse mail hazırlama ekranı açıldığında CC alanına otomatik eklenir.</summary>
    public bool IsDefaultCc { get => _isDefaultCc; set { _isDefaultCc = value; OnPropertyChanged(); } }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    /// <summary>Kaydetme sonrası oluşan kişi — hızlı ekleme akışında otomatik seçim için.</summary>
    public MailContactDto? SavedContact { get; private set; }

    public Action? SaveCompleted { get; set; }
    public ICommand SaveCommand { get; }

    public MailContactEditViewModel(
        CreateMailContactHandler createHandler,
        UpdateMailContactHandler updateHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        SaveCommand    = new RelayCommand(async () => await ExecuteSaveAsync());
    }

    public void InitializeForEdit(MailContactDto dto)
    {
        IsEditMode    = true;
        _editTargetId = dto.Id;
        FullName      = dto.FullName;
        Email         = dto.Email;
        Company       = dto.Company ?? string.Empty;
        Description   = dto.Description ?? string.Empty;
        IsDefaultCc   = dto.IsDefaultCc;
        IsActive      = dto.IsActive;
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(IsEditMode));
    }

    /// <summary>Hızlı ekleme: mail ekranından gelen adresle formu ön doldurur.</summary>
    public void PrefillEmail(string email) => Email = email;

    private async Task ExecuteSaveAsync()
    {
        ErrorMessage = null;

        if (IsEditMode)
        {
            var result = await _updateHandler.HandleAsync(new UpdateMailContactRequest
            {
                Id          = _editTargetId!.Value,
                FullName    = FullName,
                Email       = Email,
                Company     = NullIfEmpty(Company),
                Description = NullIfEmpty(Description),
                IsDefaultCc = IsDefaultCc,
                IsActive    = IsActive
            });
            if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }
        }
        else
        {
            var result = await _createHandler.HandleAsync(new CreateMailContactRequest
            {
                FullName    = FullName,
                Email       = Email,
                Company     = NullIfEmpty(Company),
                Description = NullIfEmpty(Description),
                IsDefaultCc = IsDefaultCc
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
