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
}
