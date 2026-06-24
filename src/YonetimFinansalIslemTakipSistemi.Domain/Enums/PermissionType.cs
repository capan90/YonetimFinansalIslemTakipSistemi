namespace YonetimFinansalIslemTakipSistemi.Domain.Enums;

/// <summary>
/// Kullanıcı bazlı yetki türleri.
/// Sabit sayısal değerler: sıralama değişse bile veritabanındaki int değerler korunur.
/// </summary>
public enum PermissionType
{
    CanManageUsers        = 1,
    CanViewAuditLog       = 2,
    CanCreateTransaction  = 3,
    CanEditTransaction    = 4,
    CanDeleteTransaction  = 5,
    CanViewReports            = 6,
    CanManageExchangeRates    = 7,

    // Kargo Katip modülü yetkileri
    CanViewCargoModule         = 8,
    CanManageCargoCompanies    = 9,
    CanManageCompanyDirectory  = 10,
    CanViewIncomingCargo       = 11,
    CanManageIncomingCargo     = 12,
    CanViewOutgoingCargo       = 13,
    CanManageOutgoingCargo     = 14,
}
