namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Commands.CreateCargoCompany;

public class CreateCargoCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? TrackingUrlTemplate { get; set; }
    public string? Phone   { get; set; }
    public string? Website { get; set; }

    /// <summary>Kargo Portal Bağlantısı — yalnızca http/https, boş bırakılabilir.</summary>
    public string? PortalUrl { get; set; }

    public string? Notes   { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatedByUserId { get; set; }
}
