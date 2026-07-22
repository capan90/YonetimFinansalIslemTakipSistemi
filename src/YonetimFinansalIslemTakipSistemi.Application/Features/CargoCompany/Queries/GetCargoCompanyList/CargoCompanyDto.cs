namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Queries.GetCargoCompanyList;

public class CargoCompanyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TrackingUrlTemplate { get; set; }
    public string? Phone   { get; set; }
    public string? Website { get; set; }

    /// <summary>Kargo Portal Bağlantısı — kargo işlem ekranında "Portalı Aç" için kullanılır.</summary>
    public string? PortalUrl { get; set; }

    public string? Notes   { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
