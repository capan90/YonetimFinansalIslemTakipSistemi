using System.Windows;
using System.Windows.Controls;

using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.WhatsApp;

#region Legacy - Shell Migration

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
[Obsolete(LegacyShellMigration.Reason)]
public partial class WhatsAppContactListWindow : Window
{
    public WhatsAppContactListWindow(IServiceProvider services)
    {
        InitializeComponent();
        ScreenHost.Content = new WhatsAppContactListScreen(services);
    }
}

#endregion
