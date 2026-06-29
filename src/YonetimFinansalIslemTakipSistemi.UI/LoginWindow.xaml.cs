using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Login;

namespace YonetimFinansalIslemTakipSistemi.UI;

public partial class LoginWindow : Window
{
    public LoginWindow(IServiceProvider services)
    {
        InitializeComponent();
        var vm = services.GetRequiredService<LoginViewModel>();
        DataContext = vm;

        // PasswordBox binding desteklemiyor; code-behind ile yönetilir
        PasswordBox.PasswordChanged += (_, _) =>
        {
            vm.Password = PasswordBox.Password;
            PasswordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordBox.Password)
                ? Visibility.Visible : Visibility.Collapsed;
        };

        UserNameBox.TextChanged += (_, _) =>
        {
            UserNamePlaceholder.Visibility = string.IsNullOrWhiteSpace(UserNameBox.Text)
                ? Visibility.Visible : Visibility.Collapsed;
        };

        vm.LoginCompleted = () => { DialogResult = true; };

        LoadLogo();
        SetVersionText(services.GetRequiredService<IUpdateService>());
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
