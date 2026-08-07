using System.Windows;
using System.Windows.Controls;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Users;

/// <summary>
/// UserManagementWindow — ince barindirici (Faz D6).
///
/// Icerigin tamami <see cref="UserManagementScreen"/>'de. Bu pencere yalnizca
/// pencere-seviyesi ozellikleri (baslik, boyut, ikon) tasir ve ekrani
/// barindirir; is mantigi kopyalanmaz.
///
/// Ayni ekran kabukta sekme olarak da aciliyor — barindiricidan bagimsiz
/// calistiginin canli kaniti.
/// </summary>
public partial class UserManagementWindow : Window
{
    public UserManagementWindow(IServiceProvider services)
    {
        InitializeComponent();
        ScreenHost.Content = new UserManagementScreen(services);
    }
}
