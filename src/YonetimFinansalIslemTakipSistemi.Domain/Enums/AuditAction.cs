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
    MailSettingsUpdated
}
