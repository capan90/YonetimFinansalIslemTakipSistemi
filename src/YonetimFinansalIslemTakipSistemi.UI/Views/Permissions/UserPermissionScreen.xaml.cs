using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Permissions;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Permissions;

public partial class UserPermissionScreen : UserControl
{
    private readonly UserPermissionViewModel _vm;

    public UserPermissionScreen(IServiceProvider services)
    {
        InitializeComponent();
        _vm       = services.GetRequiredService<UserPermissionViewModel>();
        DataContext = _vm;

        ScreenData.Bind(this, () => _vm.LoadAsync());
    }
}
