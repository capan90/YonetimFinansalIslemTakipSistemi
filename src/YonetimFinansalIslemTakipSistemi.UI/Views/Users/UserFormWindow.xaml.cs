using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Features.Users.Queries.GetUsers;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Users;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Users;

public partial class UserFormWindow : Window
{
    public UserFormWindow(IServiceProvider services, UserDto? editTarget = null)
    {
        InitializeComponent();
        var vm = services.GetRequiredService<UserFormViewModel>();
        vm.Initialize(editTarget);
        DataContext = vm;

        // PasswordBox data-binding desteklemez; değişiklik code-behind üzerinden iletilir
        PasswordBox.PasswordChanged += (_, _) => vm.Password = PasswordBox.Password;

        vm.SaveCompleted = () => { DialogResult = true; };
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}
