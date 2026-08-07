using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Analysis;
using YonetimFinansalIslemTakipSistemi.UI.Views.Analysis;
using YonetimFinansalIslemTakipSistemi.UI.Views.AuditLogs;
using YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;
using YonetimFinansalIslemTakipSistemi.UI.Views.CashTransactions;
using YonetimFinansalIslemTakipSistemi.UI.Views.ExchangeRates;
using YonetimFinansalIslemTakipSistemi.UI.Views.Health;
using YonetimFinansalIslemTakipSistemi.UI.Views.Mail;
using YonetimFinansalIslemTakipSistemi.UI.Views.Permissions;
using YonetimFinansalIslemTakipSistemi.UI.Views.Reports;
using YonetimFinansalIslemTakipSistemi.UI.Views.SystemLogs;
using YonetimFinansalIslemTakipSistemi.UI.Views.Users;
using YonetimFinansalIslemTakipSistemi.UI.Views.WhatsApp;

namespace YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

/// <summary>
/// Kabukta sekme olarak açılabilen ekranların kayıt tablosu.
///
/// YETKİ EŞLEMESİ VARSAYIMA DAYANMAZ. Her satır, ekranın BUGÜN mevcut
/// uygulamadaki görünürlük kapısından birebir çıkarıldı:
///   MainWindow.RefreshMenuVisibility()
///   CargoDashboardWindow.ApplyNavBarVisibility()
///   App.ResolveStartupMode()
/// Eşleme <c>ScreenRegistryPermissionTests</c> içinde satır satır sabitlenmiştir;
/// kaynak koddaki kapı değişirse test ile birlikte güncellenmelidir.
///
/// TAŞIMA DURUMU: Faz D6 itibarıyla ekranların TAMAMI taşındı ve her satırın
/// fabrikası dolu. Fabrikası boş bir satır navigasyon rayında GÖRÜNMEZ ve
/// programatik olarak da açılamaz (bkz. ShellViewModel.Resolve) — menüler
/// kabuğa taşındığı için böyle bir ekran sessizce erişilemez hâle gelirdi.
/// <c>ShellViewModelTests.Kayit_tablosundaki_tum_ekranlar_tasinmis</c> bunu
/// sabitler.
///
/// NAVGROUP menü çubuğunun karşılığıdır: kullanıcının alıştığı gruplama
/// (Finans / Kargo Takip / Yönetim / Ayarlar) rayda korunur.
/// </summary>
public static class ScreenRegistry
{
    // Navigasyon rayı başlıkları — MainWindow'un menü çubuğundan birebir
    // alındı ki kullanıcının bildiği gruplama korunsun.
    private const string Finans  = "Finans";
    private const string Kargo   = "Kargo Takip";
    private const string Yonetim = "Yönetim";
    private const string Ayarlar = "Ayarlar";

    /// <summary>
    /// "Finans kullanıcısı" tanımı — App.ResolveStartupMode ile AYNI küme.
    /// Bugün bu kümedeki bir yetkiye sahip kullanıcı MainWindow'u, dolayısıyla
    /// nakit işlem listesini görüyor. Kabuk aynı davranışı sürdürür.
    /// </summary>
    private static readonly PermissionType[] FinanceAccess =
    [
        PermissionType.CanCreateTransaction,
        PermissionType.CanEditTransaction,
        PermissionType.CanDeleteTransaction,
        PermissionType.CanViewReports,
        PermissionType.CanManageUsers,
        PermissionType.CanViewAuditLog,
        PermissionType.CanManageExchangeRates,
    ];

    /// <summary>
    /// "Kargo kullanıcısı" tanımı — App.ResolveStartupMode ve
    /// MainWindow'daki Kargo Takip menüsünün kapısı ile AYNI küme.
    /// WhatsApp ve Mail rehberi bugün ayrı bir kapı taşımıyor; kargo
    /// menüsü görünüyorsa görünüyorlar.
    /// </summary>
    private static readonly PermissionType[] CargoAccess =
    [
        PermissionType.CanViewCargoModule,
        PermissionType.CanViewIncomingCargo,
        PermissionType.CanManageIncomingCargo,
        PermissionType.CanViewOutgoingCargo,
        PermissionType.CanManageOutgoingCargo,
        PermissionType.CanManageCompanyDirectory,
        PermissionType.CanManageCargoCompanies,
    ];

    public static IReadOnlyList<ScreenDefinition> All { get; } =
    [
        // ── Finans ────────────────────────────────────────────────────────
        // Nakit İşlemler kapatılamaz: finans kullanıcısının ana çalışma alanı.
        //
        // TAŞINMIŞ (Faz D4). Ekranın kurucusu IServiceProvider alır ve kendi
        // ViewModel'ini oradan çözer — kabuk ekranın içine karışmaz.
        new(ScreenKey.CashTransactions, "Nakit İşlemler", FinanceAccess,
            CreateView: services => new CashTransactionsScreen(services),
            CanClose: false,
            NavGroup: Finans),

        new(ScreenKey.Analysis, "Finans Analiz", [PermissionType.CanViewReports],
            // Analiz ekranı ViewModel'ini kurucudan alır (mevcut sözleşme);
            // fabrikanın işi o farkı kapatmak.
            CreateView: services => new AnalysisScreen(services.GetRequiredService<AnalysisViewModel>()),
            NavGroup: Finans),

        new(ScreenKey.Reports, "Raporlar", [PermissionType.CanViewReports],
            CreateView: services => new ReportScreen(services),
            NavGroup: Finans),

        new(ScreenKey.ExchangeRates, "Döviz Kurları", [PermissionType.CanManageExchangeRates],
            CreateView: services => new ExchangeRateScreen(services),
            NavGroup: Finans),

        // ── Kargo ─────────────────────────────────────────────────────────
        new(ScreenKey.CargoDashboard, "Kargo Dashboard", [PermissionType.CanViewCargoModule],
            CreateView: services => new CargoDashboardScreen(services),
            NavGroup: Kargo),

        new(ScreenKey.IncomingCargo, "Gelen Kargolar",
            [PermissionType.CanViewIncomingCargo, PermissionType.CanManageIncomingCargo],
            CreateView: services => new CargoShipmentListScreen(services, CargoShipmentDirection.Incoming),
            NavGroup: Kargo),

        new(ScreenKey.OutgoingCargo, "Giden Kargolar",
            [PermissionType.CanViewOutgoingCargo, PermissionType.CanManageOutgoingCargo],
            CreateView: services => new CargoShipmentListScreen(services, CargoShipmentDirection.Outgoing),
            NavGroup: Kargo),

        // PARAMETRELİ: seçili bir kargo üzerinde çalışır, navigasyon rayında
        // görünmez. Kargo listesindeki "Operasyon" butonundan açılır; farklı
        // kargolar ayrı sekmelerde durabilir.
        // Yetki kapısı listeyi açan kapının aynısı — operasyon o listeden
        // erişilen bir eylem.
        new(ScreenKey.CargoOperationCenter, "Operasyon Merkezi",
            [
                PermissionType.CanViewIncomingCargo, PermissionType.CanManageIncomingCargo,
                PermissionType.CanViewOutgoingCargo, PermissionType.CanManageOutgoingCargo,
            ],
            CreateInstance: CreateOperationCenter,
            IsParameterized: true,
            NavGroup: Kargo),

        new(ScreenKey.CompanyDirectory, "Firma Rehberi",
            [PermissionType.CanManageCompanyDirectory, PermissionType.CanViewCargoModule],
            CreateView: services => new CompanyDirectoryListScreen(services),
            NavGroup: Kargo),

        new(ScreenKey.CargoCompanies, "Kargo Firmaları",
            [PermissionType.CanManageCargoCompanies, PermissionType.CanViewCargoModule],
            CreateView: services => new CargoCompanyListScreen(services),
            NavGroup: Kargo),

        // Ortak rehberler: bugün ayrı kapıları yok, kargo kullanıcısına açık.
        // Yazma işlemleri handler guard'ı ve liste ekranındaki buton
        // görünürlüğüyle korunuyor (bkz. ApplyNavBarVisibility yorumu).
        new(ScreenKey.WhatsAppContacts, "WhatsApp Rehberi", CargoAccess,
            CreateView: services => new WhatsAppContactListScreen(services),
            NavGroup: Kargo),

        new(ScreenKey.MailContacts, "Mail Rehberi", CargoAccess,
            CreateView: services => new MailContactListScreen(services),
            NavGroup: Kargo),

        // ── Yönetim / sistem ──────────────────────────────────────────────
        new(ScreenKey.Users, "Kullanıcılar", [PermissionType.CanManageUsers],
            CreateView: services => new UserManagementScreen(services),
            NavGroup: Yonetim),

        new(ScreenKey.Permissions, "Yetkiler", [PermissionType.CanManageUsers],
            CreateView: services => new UserPermissionScreen(services),
            NavGroup: Yonetim),

        new(ScreenKey.AuditLog, "Denetim Günlüğü", [PermissionType.CanViewAuditLog],
            CreateView: services => new AuditLogScreen(services),
            NavGroup: Yonetim),

        new(ScreenKey.SystemLogs, "Sistem Logları", [PermissionType.CanViewSystemLogs],
            CreateView: services => new SystemLogsScreen(services),
            NavGroup: Ayarlar),

        // Sistem Sağlığı iki kapıdan da açılabiliyor (canSystemLogs || canAccessSettings).
        // İlk taslakta CanManageUsers varsaymıştım — mevcut kod öyle demiyor.
        new(ScreenKey.SystemHealth, "Sistem Sağlığı",
            [PermissionType.CanViewSystemLogs, PermissionType.CanAccessSettings],
            CreateView: services => new SystemHealthScreen(services),
            NavGroup: Ayarlar),
    ];

    /// <summary>
    /// Operasyon Merkezi örneği. Kimlik kargo kaydının Id'sidir: aynı kargo
    /// ikinci kez açılınca yeni sekme değil, mevcut sekme öne gelir.
    /// Başlık ekranın kendisinden alınır — kayda özgü metni o üretiyor.
    /// </summary>
    private static ScreenInstance CreateOperationCenter(IServiceProvider services, object parameter)
    {
        if (parameter is not CargoShipmentDto dto)
            throw new ArgumentException(
                $"Operasyon Merkezi {nameof(CargoShipmentDto)} bekler, {parameter.GetType().Name} geldi.",
                nameof(parameter));

        var screen = new CargoOperationCenterScreen(services, dto);
        return new ScreenInstance(dto.Id.ToString(), screen.ScreenTitle, screen);
    }
}
