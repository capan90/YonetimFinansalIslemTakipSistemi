using YonetimFinansalIslemTakipSistemi.Domain.Enums;

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
/// TAŞIMA DURUMU: Bu tabloda ekranların tamamı tarif edilir ama
/// <see cref="ScreenDefinition.CreateView"/> yalnızca UserControl'e taşınmış
/// olanlarda doludur. Faz D'nin bu adımında HİÇBİR ekran taşınmadı.
/// Sahte/boş görünüm üretilmedi.
/// </summary>
public static class ScreenRegistry
{
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
        new(ScreenKey.CashTransactions, "Nakit İşlemler", FinanceAccess, CanClose: false),
        new(ScreenKey.Analysis,         "Finans Analiz",  [PermissionType.CanViewReports]),
        new(ScreenKey.Reports,          "Raporlar",       [PermissionType.CanViewReports]),
        new(ScreenKey.ExchangeRates,    "Döviz Kurları",  [PermissionType.CanManageExchangeRates]),

        // ── Kargo ─────────────────────────────────────────────────────────
        new(ScreenKey.CargoDashboard, "Kargo Dashboard", [PermissionType.CanViewCargoModule]),

        new(ScreenKey.IncomingCargo, "Gelen Kargolar",
            [PermissionType.CanViewIncomingCargo, PermissionType.CanManageIncomingCargo]),

        new(ScreenKey.OutgoingCargo, "Giden Kargolar",
            [PermissionType.CanViewOutgoingCargo, PermissionType.CanManageOutgoingCargo]),

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
            IsParameterized: true),

        new(ScreenKey.CompanyDirectory, "Firma Rehberi",
            [PermissionType.CanManageCompanyDirectory, PermissionType.CanViewCargoModule]),

        new(ScreenKey.CargoCompanies, "Kargo Firmaları",
            [PermissionType.CanManageCargoCompanies, PermissionType.CanViewCargoModule]),

        // Ortak rehberler: bugün ayrı kapıları yok, kargo kullanıcısına açık.
        // Yazma işlemleri handler guard'ı ve liste ekranındaki buton
        // görünürlüğüyle korunuyor (bkz. ApplyNavBarVisibility yorumu).
        new(ScreenKey.WhatsAppContacts, "WhatsApp Rehberi", CargoAccess),
        new(ScreenKey.MailContacts,     "Mail Rehberi",     CargoAccess),

        // ── Yönetim / sistem ──────────────────────────────────────────────
        new(ScreenKey.Users,       "Kullanıcılar",    [PermissionType.CanManageUsers]),
        new(ScreenKey.Permissions, "Yetkiler",        [PermissionType.CanManageUsers]),
        new(ScreenKey.AuditLog,    "Denetim Günlüğü", [PermissionType.CanViewAuditLog]),
        new(ScreenKey.SystemLogs,  "Sistem Logları",  [PermissionType.CanViewSystemLogs]),

        // Sistem Sağlığı iki kapıdan da açılabiliyor (canSystemLogs || canAccessSettings).
        // İlk taslakta CanManageUsers varsaymıştım — mevcut kod öyle demiyor.
        new(ScreenKey.SystemHealth, "Sistem Sağlığı",
            [PermissionType.CanViewSystemLogs, PermissionType.CanAccessSettings]),
    ];
}
