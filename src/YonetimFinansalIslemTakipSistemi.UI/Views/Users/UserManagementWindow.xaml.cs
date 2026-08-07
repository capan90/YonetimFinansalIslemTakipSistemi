using System.Windows;
using System.Windows.Controls;

using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Users;

#region Legacy - Shell Migration

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
[Obsolete(LegacyShellMigration.Reason)]
public partial class UserManagementWindow : Window
{
    public UserManagementWindow(IServiceProvider services)
    {
        InitializeComponent();
        ScreenHost.Content = new UserManagementScreen(services);
    }
}

#endregion
