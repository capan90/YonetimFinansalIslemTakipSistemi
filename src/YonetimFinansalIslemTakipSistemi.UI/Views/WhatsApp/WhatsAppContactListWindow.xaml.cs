using System.Windows;
using System.Windows.Controls;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.WhatsApp;

/// <summary>
/// WhatsAppContactListWindow — ince barindirici (Faz D6).
///
/// Icerigin tamami <see cref="WhatsAppContactListScreen"/>'de. Bu pencere yalnizca
/// pencere-seviyesi ozellikleri (baslik, boyut, ikon) tasir ve ekrani
/// barindirir; is mantigi kopyalanmaz.
///
/// Ayni ekran kabukta sekme olarak da aciliyor — barindiricidan bagimsiz
/// calistiginin canli kaniti.
/// </summary>
public partial class WhatsAppContactListWindow : Window
{
    public WhatsAppContactListWindow(IServiceProvider services)
    {
        InitializeComponent();
        ScreenHost.Content = new WhatsAppContactListScreen(services);
    }
}
