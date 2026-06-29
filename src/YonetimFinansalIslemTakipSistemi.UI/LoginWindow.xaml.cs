using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Login;

namespace YonetimFinansalIslemTakipSistemi.UI;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;
    private readonly ILocalUserPreferencesService _prefService;
    private readonly ISystemLogService? _logService;

    public LoginWindow(IServiceProvider services)
    {
        InitializeComponent();

        _vm         = services.GetRequiredService<LoginViewModel>();
        _prefService = services.GetRequiredService<ILocalUserPreferencesService>();
        _logService  = services.GetService<ISystemLogService>();
        DataContext  = _vm;

        // PasswordBox binding desteklemiyor; code-behind ile yönetilir
        PasswordBox.PasswordChanged += (_, _) =>
        {
            _vm.Password = PasswordBox.Password;
            PasswordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordBox.Password)
                ? Visibility.Visible : Visibility.Collapsed;
        };

        UserNameBox.TextChanged += (_, _) =>
        {
            UserNamePlaceholder.Visibility = string.IsNullOrWhiteSpace(UserNameBox.Text)
                ? Visibility.Visible : Visibility.Collapsed;
        };

        // Başarılı login: username kaydedilir, ardından pencere kapanır.
        // Func<Task> — await ile yazma tamamlanmadan DialogResult=true set edilmez.
        _vm.LoginCompleted = async () =>
        {
            if (!string.IsNullOrWhiteSpace(_vm.UserName))
            {
                try
                {
                    await _prefService.SaveLastUsernameAsync(_vm.UserName);
                }
                catch (Exception ex)
                {
                    // Kayıt hatası başarılı girişi engellememeli — sadece logla
                    _ = _logService?.LogWarningAsync(
                        "Login",
                        $"Son kullanıcı adı kaydedilemedi: {ex.Message}",
                        source: "LoginWindow");
                }
            }
            DialogResult = true;
        };

        Loaded += async (_, _) => await RestoreLastUsernameAsync();

        LoadLogo();
        SetVersionText(services.GetRequiredService<IUpdateService>());
    }

    /// <summary>
    /// Son başarılı giriş kullanıcı adını doldurur ve odağı ayarlar.
    /// Dosya okunamazsa sessizce geçer — login ekranı patlamaz.
    /// </summary>
    private async Task RestoreLastUsernameAsync()
    {
        var lastUsername = await _prefService.GetLastUsernameAsync();
        if (!string.IsNullOrWhiteSpace(lastUsername))
        {
            // ViewModel property'si set edilir → TwoWay binding UserNameBox.Text'i günceller
            // → TextChanged → placeholder gizlenir
            _vm.UserName = lastUsername;
            PasswordBox.Focus();
        }
        else
        {
            UserNameBox.Focus();
        }
    }

    private void LoadLogo()
    {
        try
        {
            AppLogo.Source = new BitmapImage(
                new Uri("pack://application:,,,/Assets/LoginIcon.png"));
        }
        catch
        {
            AppLogo.Visibility = Visibility.Collapsed;
        }
    }

    private void SetVersionText(IUpdateService updateService)
    {
        // ClickOnce deployment ise assembly versiyonu publish versiyonunu yansıtır.
        // Dev ortamında (doğrudan exe) "Development" gösterilir.
        if (updateService.IsClickOnceDeployment)
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            VersionText.Text = v is not null
                ? $"Versiyon: {v.ToString(4)}"
                : "Versiyon: —";
        }
        else
        {
            VersionText.Text = "Versiyon: Development";
        }
    }
}
