using System.Windows;
using System.Windows.Controls;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Reports;

/// <summary>
/// ReportWindow — ince barindirici (Faz D6).
///
/// Icerigin tamami <see cref="ReportScreen"/>'de. Bu pencere yalnizca
/// pencere-seviyesi ozellikleri (baslik, boyut, ikon) tasir ve ekrani
/// barindirir; is mantigi kopyalanmaz.
///
/// Ayni ekran kabukta sekme olarak da aciliyor — barindiricidan bagimsiz
/// calistiginin canli kaniti.
/// </summary>
public partial class ReportWindow : Window
{
    public ReportWindow(IServiceProvider services)
    {
        InitializeComponent();
        ScreenHost.Content = new ReportScreen(services);
    }
}
