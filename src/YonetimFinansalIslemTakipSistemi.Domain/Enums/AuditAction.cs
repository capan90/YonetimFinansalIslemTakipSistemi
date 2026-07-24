namespace YonetimFinansalIslemTakipSistemi.Domain.Enums;

public enum AuditAction
{
    TransactionCreated,
    TransactionUpdated,
    TransactionDeleted,
    UserCreated,
    UserUpdated,
    UserDeleted,
    UserLoggedIn,
    PermissionUpdated,
    ExchangeRateCreated,
    ExchangeRateUpdated,

    // Kargo Katip modülü
    CompanyDirectoryCreated,
    CompanyDirectoryUpdated,
    CompanyDirectoryDeleted,
    CargoCompanyCreated,
    CargoCompanyUpdated,
    CargoCompanyDeleted,
    CargoShipmentCreated,
    CargoShipmentUpdated,
    CargoShipmentDeleted,

    // Kargo operasyon aksiyonları — Sprint 3.2/3.3/3.4'te handler'lar tarafından kullanılır
    CargoLabelPrinted,
    CargoWhatsAppPrepared,
    CargoMailPrepared,

    // Ayarlar modülü
    MailSettingsUpdated,

    // Kullanıcı tercihleri — harf duyarlılığı vb.
    UserPreferenceUpdated,

    // WhatsApp rehberi (ortak)
    WhatsAppContactCreated,
    WhatsAppContactUpdated,
    WhatsAppContactDeleted,

    // DİKKAT: Action kolonu int saklanır — yeni değerler HER ZAMAN sona eklenir,
    // araya ekleme mevcut kayıtların anlamını kaydırır.
    UserLoggedOut,

    // Finansal veri dışa aktarımı (PDF/Excel) — uyum/izlenebilirlik gereği denetlenir
    ReportExported,

    // Kargo toplu içe aktarma (Excel vb. dosyadan) — özet kaydı; satır bazlı
    // kayıtlar ayrıca CargoShipmentCreated olarak yazılır
    CargoImportCompleted,

    // Firma rehberi toplu içe aktarma özeti (satırlar CompanyDirectoryCreated)
    DirectoryImportCompleted,

    // WhatsApp rehberi toplu içe aktarma özeti (satırlar WhatsAppContactCreated/Updated)
    WhatsAppImportCompleted
}
