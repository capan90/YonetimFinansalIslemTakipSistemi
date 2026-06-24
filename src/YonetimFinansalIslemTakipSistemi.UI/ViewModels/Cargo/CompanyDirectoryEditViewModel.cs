using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Commands.CreateCompanyDirectory;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Commands.UpdateCompanyDirectory;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Queries.GetCompanyDirectoryList;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;

public class CompanyDirectoryEditViewModel : INotifyPropertyChanged
{
    private readonly CreateCompanyDirectoryHandler _createHandler;
    private readonly UpdateCompanyDirectoryHandler _updateHandler;
    private readonly IUserContext _userContext;

    private Guid? _editTargetId;
    private string _companyName   = string.Empty;
    private string _contactPerson = string.Empty;
    private string _attentionTo   = string.Empty;
    private string _addressLine   = string.Empty;
    private string _district      = string.Empty;
    private string _city          = string.Empty;
    private string _postalCode    = string.Empty;
    private string _phone         = string.Empty;
    private string _email         = string.Empty;
    private string _notes         = string.Empty;
    private bool   _isActive      = true;
    private string? _errorMessage;

    public bool IsEditMode { get; private set; }
    public string WindowTitle => IsEditMode ? "Firma Düzenle" : "Yeni Firma";

    public string CompanyName   { get => _companyName;   set { _companyName   = value; OnPropertyChanged(); } }
    public string ContactPerson { get => _contactPerson; set { _contactPerson = value; OnPropertyChanged(); } }
    public string AttentionTo   { get => _attentionTo;   set { _attentionTo   = value; OnPropertyChanged(); } }
    public string AddressLine   { get => _addressLine;   set { _addressLine   = value; OnPropertyChanged(); } }
    public string District      { get => _district;      set { _district      = value; OnPropertyChanged(); } }
    public string City          { get => _city;          set { _city          = value; OnPropertyChanged(); } }
    public string PostalCode    { get => _postalCode;    set { _postalCode    = value; OnPropertyChanged(); } }
    public string Phone         { get => _phone;         set { _phone         = value; OnPropertyChanged(); } }
    public string Email         { get => _email;         set { _email         = value; OnPropertyChanged(); } }
    public string Notes         { get => _notes;         set { _notes         = value; OnPropertyChanged(); } }
    public bool IsActive        { get => _isActive;      set { _isActive      = value; OnPropertyChanged(); } }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public Action? SaveCompleted { get; set; }
    public ICommand SaveCommand { get; }

    public CompanyDirectoryEditViewModel(
        CreateCompanyDirectoryHandler createHandler,
        UpdateCompanyDirectoryHandler updateHandler,
        IUserContext userContext)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _userContext   = userContext;
        SaveCommand    = new RelayCommand(async () => await ExecuteSaveAsync());
    }

    public void Initialize(CompanyDirectoryDto dto)
    {
        IsEditMode    = true;
        _editTargetId = dto.Id;
        CompanyName   = dto.CompanyName;
        ContactPerson = dto.ContactPerson ?? string.Empty;
        AttentionTo   = dto.AttentionTo   ?? string.Empty;
        AddressLine   = dto.AddressLine;
        District      = dto.District      ?? string.Empty;
        City          = dto.City          ?? string.Empty;
        PostalCode    = dto.PostalCode    ?? string.Empty;
        Phone         = dto.Phone         ?? string.Empty;
        Email         = dto.Email         ?? string.Empty;
        Notes         = dto.Notes         ?? string.Empty;
        IsActive      = dto.IsActive;
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(IsEditMode));
    }

    private async Task ExecuteSaveAsync()
    {
        ErrorMessage = null;

        if (IsEditMode)
        {
            var req = new UpdateCompanyDirectoryRequest
            {
                Id            = _editTargetId!.Value,
                CompanyName   = CompanyName,
                ContactPerson = NullIfEmpty(ContactPerson),
                AttentionTo   = NullIfEmpty(AttentionTo),
                AddressLine   = AddressLine,
                District      = NullIfEmpty(District),
                City          = NullIfEmpty(City),
                PostalCode    = NullIfEmpty(PostalCode),
                Phone         = NullIfEmpty(Phone),
                Email         = NullIfEmpty(Email),
                Notes         = NullIfEmpty(Notes),
                IsActive      = IsActive,
                UpdatedByUserId = _userContext.UserId
            };
            var result = await _updateHandler.HandleAsync(req);
            if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }
        }
        else
        {
            var req = new CreateCompanyDirectoryRequest
            {
                CompanyName   = CompanyName,
                ContactPerson = NullIfEmpty(ContactPerson),
                AttentionTo   = NullIfEmpty(AttentionTo),
                AddressLine   = AddressLine,
                District      = NullIfEmpty(District),
                City          = NullIfEmpty(City),
                PostalCode    = NullIfEmpty(PostalCode),
                Phone         = NullIfEmpty(Phone),
                Email         = NullIfEmpty(Email),
                Notes         = NullIfEmpty(Notes),
                IsActive      = IsActive,
                CreatedByUserId = _userContext.UserId
            };
            var result = await _createHandler.HandleAsync(req);
            if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }
        }

        SaveCompleted?.Invoke();
    }

    private static string? NullIfEmpty(string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
