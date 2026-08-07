using System.Windows;
using System.Windows.Controls;

using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.ExchangeRates;

#region Legacy - Shell Migration

/// <summary>
/// ExchangeRateWindow — ince barindirici (Faz D6).
///
/// Icerigin tamami <see cref="ExchangeRateScreen"/>'de. Bu pencere yalnizca
/// pencere-seviyesi ozellikleri (baslik, boyut, ikon) tasir ve ekrani
/// barindirir; is mantigi kopyalanmaz.
///
/// Ayni ekran kabukta sekme olarak da aciliyor — barindiricidan bagimsiz
/// calistiginin canli kaniti.
/// </summary>
[Obsolete(LegacyShellMigration.Reason)]
public partial class ExchangeRateWindow : Window
{
    public ExchangeRateWindow(IServiceProvider services)
    {
        InitializeComponent();
        ScreenHost.Content = new ExchangeRateScreen(services);
    }
}

#endregion
