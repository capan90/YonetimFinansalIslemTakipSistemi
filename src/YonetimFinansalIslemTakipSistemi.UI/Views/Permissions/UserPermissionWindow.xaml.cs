using System.Windows;
using System.Windows.Controls;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Permissions;

/// <summary>
/// UserPermissionWindow — ince barindirici (Faz D6).
///
/// Icerigin tamami <see cref="UserPermissionScreen"/>'de. Bu pencere yalnizca
/// pencere-seviyesi ozellikleri (baslik, boyut, ikon) tasir ve ekrani
/// barindirir; is mantigi kopyalanmaz.
///
/// Ayni ekran kabukta sekme olarak da aciliyor — barindiricidan bagimsiz
/// calistiginin canli kaniti.
/// </summary>
public partial class UserPermissionWindow : Window
{
    public UserPermissionWindow(IServiceProvider services)
    {
        InitializeComponent();
        ScreenHost.Content = new UserPermissionScreen(services);
    }
}
