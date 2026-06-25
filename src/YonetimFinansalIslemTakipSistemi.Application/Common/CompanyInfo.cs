namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// Gönderici firma bilgileri. Şimdilik AppSettings'ten okunur;
/// ileride Company Settings modülünden beslenecek şekilde yapı hazırdır.
/// </summary>
public record CompanyInfo(
    string  Name,
    string? Address,
    string? District,
    string? City,
    string? Phone,
    string? LogoPath
);
