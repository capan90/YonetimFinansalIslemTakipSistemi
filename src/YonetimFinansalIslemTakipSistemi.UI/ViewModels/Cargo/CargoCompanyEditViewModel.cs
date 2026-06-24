using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Commands.CreateCargoCompany;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Commands.UpdateCargoCompany;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Queries.GetCargoCompanyList;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;

public class CargoCompanyEditViewModel : INotifyPropertyChanged
{
    private readonly CreateCargoCompanyHandler _createHandler;
    private readonly UpdateCargoCompanyHandler _updateHandler;
    private readonly IUserContext _userContext;

    private Guid? _editTargetId;
    private string _name                = string.Empty;
    private string _trackingUrlTemplate = string.Empty;
    private string _phone               = string.Empty;
    private string _website             = string.Empty;
    private string _notes               = string.Empty;
    private bool   _isActive            = true;
    private string? _errorMessage;

    public bool IsEditMode { get; private set; }
    public string WindowTitle => IsEditMode ? "Kargo Firması Düzenle" : "Yeni Kargo Firması";

    public string Name                { get => _name;                set { _name                = value; OnPropertyChanged(); } }
    public string TrackingUrlTemplate { get => _trackingUrlTemplate; set { _trackingUrlTemplate = value; OnPropertyChanged(); } }
    public string Phone               { get => _phone;               set { _phone               = value; OnPropertyChanged(); } }
    public string Website             { get => _website;             set { _website             = value; OnPropertyChanged(); } }
    public string Notes               { get => _notes;               set { _notes               = value; OnPropertyChanged(); } }
    public bool IsActive              { get => _isActive;            set { _isActive            = value; OnPropertyChanged(); } }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public Action? SaveCompleted { get; set; }
    public ICommand SaveCommand { get; }

    public CargoCompanyEditViewModel(
        CreateCargoCompanyHandler createHandler,
        UpdateCargoCompanyHandler updateHandler,
        IUserContext userContext)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _userContext   = userContext;
        SaveCommand    = new RelayCommand(async () => await ExecuteSaveAsync());
    }

    public void Initialize(CargoCompanyDto dto)
    {
        IsEditMode          = true;
        _editTargetId       = dto.Id;
        Name                = dto.Name;
        TrackingUrlTemplate = dto.TrackingUrlTemplate ?? string.Empty;
        Phone               = dto.Phone               ?? string.Empty;
        Website             = dto.Website             ?? string.Empty;
        Notes               = dto.Notes               ?? string.Empty;
        IsActive            = dto.IsActive;
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(IsEditMode));
    }

    private async Task ExecuteSaveAsync()
    {
        ErrorMessage = null;

        if (IsEditMode)
        {
            var req = new UpdateCargoCompanyRequest
            {
                Id                  = _editTargetId!.Value,
                Name                = Name,
                TrackingUrlTemplate = NullIfEmpty(TrackingUrlTemplate),
                Phone               = NullIfEmpty(Phone),
                Website             = NullIfEmpty(Website),
                Notes               = NullIfEmpty(Notes),
                IsActive            = IsActive,
                UpdatedByUserId     = _userContext.UserId
            };
            var result = await _updateHandler.HandleAsync(req);
            if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }
        }
        else
        {
            var req = new CreateCargoCompanyRequest
            {
                Name                = Name,
                TrackingUrlTemplate = NullIfEmpty(TrackingUrlTemplate),
                Phone               = NullIfEmpty(Phone),
                Website             = NullIfEmpty(Website),
                Notes               = NullIfEmpty(Notes),
                IsActive            = IsActive,
                CreatedByUserId     = _userContext.UserId
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
