using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Analysis;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;
using YonetimFinansalIslemTakipSistemi.UI.Views.Analysis;
using YonetimFinansalIslemTakipSistemi.UI.Views.AuditLogs;
using YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;
using YonetimFinansalIslemTakipSistemi.UI.Views.CashTransactions;
using YonetimFinansalIslemTakipSistemi.UI.Views.ExchangeRates;
using YonetimFinansalIslemTakipSistemi.UI.Views.Health;
using YonetimFinansalIslemTakipSistemi.UI.Views.Permissions;
using YonetimFinansalIslemTakipSistemi.UI.Views.Reports;
using YonetimFinansalIslemTakipSistemi.UI.Views.Users;

namespace YonetimFinansalIslemTakipSistemi.UI;

/// <summary>
/// Finans kabuğu — Faz D pilot dönüşümünden sonra.
///
/// İÇERİK BURADA DEĞİL: nakit işlemler ekranı
/// <see cref="CashTransactionsScreen"/>'e taşındı ve bu pencere onu barındırıyor.
/// Kullanıcının akışı değişmedi; App.xaml.cs hâlâ bu pencereyi açıyor.
///
/// BURADA KALAN (pencere/kabuk seviyesi):
///   menü ve menü yetki görünürlüğü, menüden açılan pencereler,
///   çıkış onayı + audit + IsLogoutRequested sözleşmesi,
///   açılışta güncelleme kontrolü,
///   klavye kısayollarının pencere seviyesinde tanımı
///
/// Kısayollar burada TANIMLI kalır ki odak menüdeyken de çalışsınlar; gövdeleri
/// ekrandaki genel metotlara YÖNLENDİRİLİR — mantık iki yerde tutulmaz.
/// </summary>
public partial class MainWindow : Window
{
    private readonly IServiceProvider       _services;
    private readonly IDialogService         _dialogService;
    private readonly CashTransactionsScreen _screen;

    public bool IsLogoutRequested { get; private set; }

    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();

        _services      = services;
        _dialogService = services.GetRequiredService<IDialogService>();

        // Ekran kod arkasında üretilir: kurucusu IServiceProvider alıyor.
        //
        // Pencere ListViewModel'i ARTIK ÇÖZMÜYOR. ViewModel Transient kayıtlı
        // (App.xaml.cs) — ayrıca çözmek ekranın gösterdiğinden BAŞKA bir örnek
        // üretirdi ve F5 görünmeyen bir listeyi filtrelerdi.
        _screen = new CashTransactionsScreen(services);
        _screen.LogoutRequested += OnLogoutRequested;
        ScreenHost.Content = _screen;

        var userContext = services.GetRequiredService<IUserContext>();

        Loaded += async (_, _) =>
        {
            // Ekranın buton görünürlüğünü de bu çağrı tazeler
            RefreshMenuVisibility(userContext);

            // Açılışta güncelleme kontrolü — ekran yüklendikten sonra, kullanıcıyı bloklamadan
            await Services.StartupUpdateChecker.RunOnceAsync(_services, _dialogService);
        };
    }

    // ── Klavye Kısayolları ────────────────────────────────────────────────────
    // Pencere seviyesinde tanımlı, ekrana yönlendirilir. Yetki ve seçim
    // kontrolleri ekrandaki metotların içindedir.

    private void Command_New(object sender, ExecutedRoutedEventArgs e)         => _screen.NewTransaction();
    private void Command_Duplicate(object sender, ExecutedRoutedEventArgs e)   => _screen.DuplicateTransaction();
    private void Command_Delete(object sender, ExecutedRoutedEventArgs e)      => _screen.DeleteSelectedTransaction();
    private void Command_ImportExcel(object sender, ExecutedRoutedEventArgs e) => _screen.ImportExcel();
    private void Command_FocusSearch(object sender, ExecutedRoutedEventArgs e) => _screen.FocusSearch();
    private void Command_Refresh(object sender, ExecutedRoutedEventArgs e)     => _screen.RefreshList();

    // ── Çıkış ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ekrandaki çıkış butonu isteği yayar; onay, audit ve pencerenin
    /// kapatılması burada — pencere seviyesi davranış.
    /// </summary>
    private async void OnLogoutRequested()
    {
        if (!Common.SessionLogout.Confirm(_dialogService)) return;

        await Common.SessionLogout.WriteAuditAsync(_services);

        IsLogoutRequested = true;
        Close();
    }

    // ── Menü Görünürlüğü ──────────────────────────────────────────────────────

    private void RefreshMenuVisibility(IUserContext userContext)
    {
        var canManage   = userContext.HasPermission(PermissionType.CanManageUsers);
        var canAudit    = userContext.HasPermission(PermissionType.CanViewAuditLog);
        var canReports  = userContext.HasPermission(PermissionType.CanViewReports);
        var canExchange = userContext.HasPermission(PermissionType.CanManageExchangeRates);

        MenuItemKullanicilar.Visibility = canManage   ? Visibility.Visible : Visibility.Collapsed;
        MenuItemYetkiler.Visibility     = canManage   ? Visibility.Visible : Visibility.Collapsed;
        MenuItemDenetim.Visibility      = canAudit    ? Visibility.Visible : Visibility.Collapsed;
        MenuItemRaporlar.Visibility     = canReports  ? Visibility.Visible : Visibility.Collapsed;
        MenuItemAnaliz.Visibility       = canReports  ? Visibility.Visible : Visibility.Collapsed;
        MenuItemDoviz.Visibility        = canExchange ? Visibility.Visible : Visibility.Collapsed;
        // DB testi, log klasörü ve sistem sağlığı — Ayarlar menüsüne taşındı, yönetici yetkisi gerekir
        MenuItemDbTest.Visibility        = canManage ? Visibility.Visible : Visibility.Collapsed;
        MenuItemLogKlasor.Visibility     = canManage ? Visibility.Visible : Visibility.Collapsed;
        MenuItemSistemSagligi.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;

        // Kargo Katip modülü — UI gizlemesi; asıl koruma handler seviyesindedir
        var canViewCargo     = userContext.HasPermission(PermissionType.CanViewCargoModule)
                            || userContext.HasPermission(PermissionType.CanViewIncomingCargo)
                            || userContext.HasPermission(PermissionType.CanViewOutgoingCargo)
                            || userContext.HasPermission(PermissionType.CanManageIncomingCargo)
                            || userContext.HasPermission(PermissionType.CanManageOutgoingCargo)
                            || userContext.HasPermission(PermissionType.CanManageCompanyDirectory)
                            || userContext.HasPermission(PermissionType.CanManageCargoCompanies);

        MenuItemKargoKatip.Visibility   = canViewCargo ? Visibility.Visible : Visibility.Collapsed;
        MenuItemGelenKargolar.Visibility = userContext.HasPermission(PermissionType.CanViewIncomingCargo)
                                        || userContext.HasPermission(PermissionType.CanManageIncomingCargo)
            ? Visibility.Visible : Visibility.Collapsed;
        MenuItemGidenKargolar.Visibility = userContext.HasPermission(PermissionType.CanViewOutgoingCargo)
                                        || userContext.HasPermission(PermissionType.CanManageOutgoingCargo)
            ? Visibility.Visible : Visibility.Collapsed;
        MenuItemFirmaRehberi.Visibility  = userContext.HasPermission(PermissionType.CanManageCompanyDirectory)
                                        || userContext.HasPermission(PermissionType.CanViewCargoModule)
            ? Visibility.Visible : Visibility.Collapsed;
        MenuItemKargoFirmalari.Visibility = userContext.HasPermission(PermissionType.CanManageCargoCompanies)
                                         || userContext.HasPermission(PermissionType.CanViewCargoModule)
            ? Visibility.Visible : Visibility.Collapsed;

        MenuItemKargoDashboard.Visibility = userContext.HasPermission(PermissionType.CanViewCargoModule)
            ? Visibility.Visible : Visibility.Collapsed;

        // Ayarlar menüsü: CanAccessSettings yetkisine sahipse göster
        var canAccessSettings = userContext.HasPermission(PermissionType.CanAccessSettings);
        var canSettings       = userContext.HasPermission(PermissionType.CanManageMailSettings);
        var canSystemLogs     = userContext.HasPermission(PermissionType.CanViewSystemLogs);
        var canSystemHealth   = canSystemLogs || canAccessSettings;

        MenuItemAyarlar.Visibility          = canAccessSettings ? Visibility.Visible : Visibility.Collapsed;
        MenuItemMailAyarlari.Visibility     = canSettings       ? Visibility.Visible : Visibility.Collapsed;
        MenuItemSistemLoglari.Visibility    = canSystemLogs     ? Visibility.Visible : Visibility.Collapsed;
        MenuItemGorunumAyarlari.Visibility  = canAccessSettings ? Visibility.Visible : Visibility.Collapsed;
        MenuItemSistemSagligi.Visibility    = canSystemHealth   ? Visibility.Visible : Visibility.Collapsed;
        MenuItemDbTest.Visibility           = canAccessSettings ? Visibility.Visible : Visibility.Collapsed;
        MenuItemLogKlasor.Visibility        = canAccessSettings ? Visibility.Visible : Visibility.Collapsed;

        // İşlem butonlarının yetki görünürlüğü EKRANIN sorumluluğu:
        // butonlar orada, kapıları da orada (bkz. RefreshPermissionVisibility).
        _screen.RefreshPermissionVisibility(userContext);
    }

    // ── Klavye Kısayolları ────────────────────────────────────────────────────
    // ── Menü Tıklamaları ─────────────────────────────────────────────────────

    private void OpenUserManagement_Click(object sender, RoutedEventArgs e)
    {
        new UserManagementWindow(_services) { Owner = this }.ShowDialog();
    }

    private void OpenAuditLog_Click(object sender, RoutedEventArgs e)
    {
        new AuditLogWindow(_services) { Owner = this }.ShowDialog();
    }

    private void OpenPermissions_Click(object sender, RoutedEventArgs e)
    {
        new UserPermissionWindow(_services) { Owner = this }.ShowDialog();
        var userContext = _services.GetRequiredService<IUserContext>();
        RefreshMenuVisibility(userContext);
    }

    private void OpenReports_Click(object sender, RoutedEventArgs e)
    {
        new ReportWindow(_services) { Owner = this }.ShowDialog();
    }

    private void OpenAnalysis_Click(object sender, RoutedEventArgs e)
    {
        var vm = _services.GetRequiredService<AnalysisViewModel>();
        new AnalysisWindow(vm) { Owner = this }.ShowDialog();
    }

    private void OpenExchangeRates_Click(object sender, RoutedEventArgs e)
    {
        new ExchangeRateWindow(_services) { Owner = this }.ShowDialog();
    }

    private void OpenLogDirectory_Click(object sender, RoutedEventArgs e)
        => Common.ToolActions.OpenLogDirectory(_dialogService);

    private async void TestDbConnection_Click(object sender, RoutedEventArgs e)
        => await Common.ToolActions.TestDatabaseAsync(_services, _dialogService);

    private void OpenSystemHealth_Click(object sender, RoutedEventArgs e)
    {
        new SystemHealthWindow(_services) { Owner = this }.ShowDialog();
    }

    // ── Kargo Katip Menü Tıklamaları ─────────────────────────────────────────

    private void OpenCargoDashboard_Click(object sender, RoutedEventArgs e)
    {
        new CargoDashboardWindow(_services) { Owner = this }.ShowDialog();
    }

    private void OpenIncomingCargo_Click(object sender, RoutedEventArgs e)
    {
        new CargoShipmentListWindow(_services, CargoShipmentDirection.Incoming) { Owner = this }.ShowDialog();
    }

    private void OpenOutgoingCargo_Click(object sender, RoutedEventArgs e)
    {
        new CargoShipmentListWindow(_services, CargoShipmentDirection.Outgoing) { Owner = this }.ShowDialog();
    }

    private void OpenCompanyDirectory_Click(object sender, RoutedEventArgs e)
    {
        new CompanyDirectoryListWindow(_services) { Owner = this }.ShowDialog();
    }

    private void OpenCargoCompanies_Click(object sender, RoutedEventArgs e)
    {
        new CargoCompanyListWindow(_services) { Owner = this }.ShowDialog();
    }

    private void OpenWhatsAppContacts_Click(object sender, RoutedEventArgs e)
    {
        new Views.WhatsApp.WhatsAppContactListWindow(_services) { Owner = this }.ShowDialog();
    }

    private void OpenMailContacts_Click(object sender, RoutedEventArgs e)
    {
        new Views.Mail.MailContactListWindow(_services) { Owner = this }.ShowDialog();
    }

    // ── Ayarlar Menü Tıklamaları ──────────────────────────────────────────────

    private void OpenMailSettings_Click(object sender, RoutedEventArgs e)
    {
        new Views.Settings.MailSettingsWindow(_services, isPersonal: false) { Owner = this }.ShowDialog();
    }

    private void OpenPersonalMailSettings_Click(object sender, RoutedEventArgs e)
    {
        new Views.Settings.MailSettingsWindow(_services, isPersonal: true) { Owner = this }.ShowDialog();
    }

    private void OpenSystemLogs_Click(object sender, RoutedEventArgs e)
    {
        new Views.SystemLogs.SystemLogsWindow(_services) { Owner = this }.ShowDialog();
    }

    private void OpenAppearanceSettings_Click(object sender, RoutedEventArgs e)
    {
        new Views.Settings.AppearanceSettingsWindow(_services) { Owner = this }.ShowDialog();
    }

    private void OpenTextCaseSettings_Click(object sender, RoutedEventArgs e)
    {
        new Views.Settings.TextCaseSettingsWindow(_services) { Owner = this }.ShowDialog();
    }

    // Akisin govdesi Common/UpdateCheckFlow icinde: ayni akis birden fazla
    // giris noktasindan baslatiliyor ve kopyalanirsa metinler/onay sirasi
    // birinde duzeltilip digerinde eski kalir.
    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        => await Common.UpdateCheckFlow.RunAsync(_services, _dialogService);
}

