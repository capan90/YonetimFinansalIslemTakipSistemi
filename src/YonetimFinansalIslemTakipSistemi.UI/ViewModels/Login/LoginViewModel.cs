using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Login;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly IAuthenticationService _authService;
    private string  _userName     = string.Empty;
    private string  _password     = string.Empty;
    private string? _errorMessage;

    public string UserName
    {
        get => _userName;
        set { _userName = value; OnPropertyChanged(); }
    }

    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Başarılı girişte tetiklenir; App.xaml.cs bu callback'i dinleyerek MainWindow'u açar.
    /// ViewModel pencere veya navigasyon referansı tutmaz.
    /// </summary>
    public Action? LoginCompleted { get; set; }

    public ICommand LoginCommand { get; }

    public LoginViewModel(IAuthenticationService authService)
    {
        _authService = authService;
        LoginCommand = new RelayCommand(async () => await ExecuteLoginAsync());
    }

    private async Task ExecuteLoginAsync()
    {
        // Boş alan validasyonu — servis çağrısından önce kontrol edilir.
        if (string.IsNullOrWhiteSpace(UserName))
        {
            ErrorMessage = "Kullanıcı adı boş olamaz.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Şifre boş olamaz.";
            return;
        }

        ErrorMessage = null;
        var result = await _authService.AuthenticateAsync(UserName, Password);

        if (result.Success)
            LoginCompleted?.Invoke();
        else
            ErrorMessage = result.ErrorMessage;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
