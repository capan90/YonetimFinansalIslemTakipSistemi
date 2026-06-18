using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.Users.Commands.CreateUser;
using YonetimFinansalIslemTakipSistemi.Application.Features.Users.Commands.UpdateUser;
using YonetimFinansalIslemTakipSistemi.Application.Features.Users.Queries.GetUsers;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Users;

public class UserFormViewModel : INotifyPropertyChanged
{
    private readonly CreateUserHandler _createHandler;
    private readonly UpdateUserHandler _updateHandler;

    private Guid? _editTargetId;
    private string _fullName  = string.Empty;
    private string _userName  = string.Empty;
    private bool   _isActive  = true;
    private string? _errorMessage;

    public bool IsEditMode { get; private set; }

    public string WindowTitle => IsEditMode ? "Kullanıcı Düzenle" : "Yeni Kullanıcı";

    // Şifre label'ı moduna göre değişir
    public string PasswordLabel => IsEditMode ? "Yeni Şifre (boş bırakılabilir):" : "Şifre:";

    public string FullName
    {
        get => _fullName;
        set { _fullName = value; OnPropertyChanged(); }
    }

    public string UserName
    {
        get => _userName;
        set { _userName = value; OnPropertyChanged(); }
    }

    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(); }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    // PasswordBox data-binding desteklemez; code-behind bu property'yi set eder
    public string Password { get; set; } = string.Empty;

    public Action? SaveCompleted { get; set; }

    public ICommand SaveCommand { get; }

    public UserFormViewModel(CreateUserHandler createHandler, UpdateUserHandler updateHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;

        SaveCommand = new RelayCommand(async () => await ExecuteSaveAsync());
    }

    public void Initialize(UserDto? target)
    {
        if (target is null)
        {
            IsEditMode = false;
            return;
        }

        IsEditMode   = true;
        _editTargetId = target.Id;
        FullName     = target.FullName;
        UserName     = target.UserName;
        IsActive     = target.IsActive;
    }

    private async Task ExecuteSaveAsync()
    {
        ErrorMessage = null;

        if (IsEditMode)
        {
            var request = new UpdateUserRequest
            {
                Id          = _editTargetId!.Value,
                FullName    = FullName,
                IsActive    = IsActive,
                NewPassword = string.IsNullOrWhiteSpace(Password) ? null : Password
            };
            var result = await _updateHandler.HandleAsync(request);
            if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }
        }
        else
        {
            var request = new CreateUserRequest
            {
                FullName = FullName,
                UserName = UserName,
                Password = Password
            };
            var result = await _createHandler.HandleAsync(request);
            if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }
        }

        SaveCompleted?.Invoke();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
