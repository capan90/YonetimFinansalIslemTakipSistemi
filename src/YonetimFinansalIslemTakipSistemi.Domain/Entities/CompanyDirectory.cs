using YonetimFinansalIslemTakipSistemi.Domain.Common;

namespace YonetimFinansalIslemTakipSistemi.Domain.Entities;

/// <summary>
/// Kargo gönderim/alım adres rehberi. Ticari firmalar için kullanılır;
/// ileride diğer modüller de bu rehberi paylaşabilir.
/// </summary>
public class CompanyDirectory : BaseEntity
{
    public string CompanyName   { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? AttentionTo   { get; set; }
    public string AddressLine   { get; set; } = string.Empty;
    public string? District      { get; set; }
    public string? City          { get; set; }
    public string? PostalCode    { get; set; }
    public string? Phone         { get; set; }
    public string? Email         { get; set; }
    public string? Notes         { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation — bu rehber kaydını kullanan kargo gönderileri
    public ICollection<CargoShipment> CargoShipments { get; set; } = [];
}
