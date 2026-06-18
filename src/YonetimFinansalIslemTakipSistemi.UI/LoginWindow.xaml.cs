using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Login;

namespace YonetimFinansalIslemTakipSistemi.UI;

public partial class LoginWindow : Window
{
    public LoginWindow(IServiceProvider services)
    {
        InitializeComponent();
        var vm = services.GetRequiredService<LoginViewModel>();
        DataContext = vm;

        // PasswordBox binding desteklemiyor; tek satır code-behind ile çözülür.
        PasswordBox.PasswordChanged += (_, _) => vm.Password = PasswordBox.Password;

        // Başarılı login: DialogResult=true → ShowDialog() true döner → App.xaml.cs MainWindow açar.
        vm.LoginCompleted = () => { DialogResult = true; };
    }
}
