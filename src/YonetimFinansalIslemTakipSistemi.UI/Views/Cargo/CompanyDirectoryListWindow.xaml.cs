using System.Windows;
using System.Windows.Controls;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

/// <summary>
/// CompanyDirectoryListWindow — ince barindirici (Faz D6).
///
/// Icerigin tamami <see cref="CompanyDirectoryListScreen"/>'de. Bu pencere yalnizca
/// pencere-seviyesi ozellikleri (baslik, boyut, ikon) tasir ve ekrani
/// barindirir; is mantigi kopyalanmaz.
///
/// Ayni ekran kabukta sekme olarak da aciliyor — barindiricidan bagimsiz
/// calistiginin canli kaniti.
/// </summary>
public partial class CompanyDirectoryListWindow : Window
{
    public CompanyDirectoryListWindow(IServiceProvider services)
    {
        InitializeComponent();
        ScreenHost.Content = new CompanyDirectoryListScreen(services);
    }
}
