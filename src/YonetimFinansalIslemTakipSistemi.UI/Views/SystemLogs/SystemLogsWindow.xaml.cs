using System.Windows;
using System.Windows.Controls;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.SystemLogs;

/// <summary>
/// SystemLogsWindow — ince barindirici (Faz D6).
///
/// Icerigin tamami <see cref="SystemLogsScreen"/>'de. Bu pencere yalnizca
/// pencere-seviyesi ozellikleri (baslik, boyut, ikon) tasir ve ekrani
/// barindirir; is mantigi kopyalanmaz.
///
/// Ayni ekran kabukta sekme olarak da aciliyor — barindiricidan bagimsiz
/// calistiginin canli kaniti.
/// </summary>
public partial class SystemLogsWindow : Window
{
    public SystemLogsWindow(IServiceProvider services)
    {
        InitializeComponent();
        ScreenHost.Content = new SystemLogsScreen(services);
    }
}
