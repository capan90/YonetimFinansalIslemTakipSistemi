using System.Windows;
using System.Windows.Controls;

using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

#region Legacy - Shell Migration

/// <summary>
/// CargoCompanyListWindow — ince barindirici (Faz D6).
///
/// Icerigin tamami <see cref="CargoCompanyListScreen"/>'de. Bu pencere yalnizca
/// pencere-seviyesi ozellikleri (baslik, boyut, ikon) tasir ve ekrani
/// barindirir; is mantigi kopyalanmaz.
///
/// Ayni ekran kabukta sekme olarak da aciliyor — barindiricidan bagimsiz
/// calistiginin canli kaniti.
/// </summary>
[Obsolete(LegacyShellMigration.Reason)]
public partial class CargoCompanyListWindow : Window
{
    public CargoCompanyListWindow(IServiceProvider services)
    {
        InitializeComponent();
        ScreenHost.Content = new CargoCompanyListScreen(services);
    }
}

#endregion
