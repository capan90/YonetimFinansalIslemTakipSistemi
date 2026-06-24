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
    CargoShipmentDeleted
}
