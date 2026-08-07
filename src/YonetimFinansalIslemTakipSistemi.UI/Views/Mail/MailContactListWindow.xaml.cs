using System.Windows;
using System.Windows.Controls;

using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Mail;

#region Legacy - Shell Migration

/// <summary>
/// MailContactListWindow — ince barindirici (Faz D6).
///
/// Icerigin tamami <see cref="MailContactListScreen"/>'de. Bu pencere yalnizca
/// pencere-seviyesi ozellikleri (baslik, boyut, ikon) tasir ve ekrani
/// barindirir; is mantigi kopyalanmaz.
///
/// Ayni ekran kabukta sekme olarak da aciliyor — barindiricidan bagimsiz
/// calistiginin canli kaniti.
/// </summary>
[Obsolete(LegacyShellMigration.Reason)]
public partial class MailContactListWindow : Window
{
    public MailContactListWindow(IServiceProvider services)
    {
        InitializeComponent();
        ScreenHost.Content = new MailContactListScreen(services);
    }
}

#endregion
