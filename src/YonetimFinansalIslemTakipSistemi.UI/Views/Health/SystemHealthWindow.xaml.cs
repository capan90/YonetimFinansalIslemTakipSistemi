using System.Windows;
using System.Windows.Controls;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Health;

/// <summary>
/// SystemHealthWindow — ince barindirici (Faz D6).
///
/// Icerigin tamami <see cref="SystemHealthScreen"/>'de. Bu pencere yalnizca
/// pencere-seviyesi ozellikleri (baslik, boyut, ikon) tasir ve ekrani
/// barindirir; is mantigi kopyalanmaz.
///
/// Ayni ekran kabukta sekme olarak da aciliyor — barindiricidan bagimsiz
/// calistiginin canli kaniti.
/// </summary>
public partial class SystemHealthWindow : Window
{
    public SystemHealthWindow(IServiceProvider services)
    {
        InitializeComponent();
        ScreenHost.Content = new SystemHealthScreen(services);
    }
}
