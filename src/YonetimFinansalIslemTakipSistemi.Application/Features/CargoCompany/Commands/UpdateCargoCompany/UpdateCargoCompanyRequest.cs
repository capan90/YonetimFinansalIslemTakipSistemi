namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Commands.UpdateCargoCompany;

public class UpdateCargoCompanyRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TrackingUrlTemplate { get; set; }
    public string? Phone   { get; set; }
    public string? Website { get; set; }

    /// <summary>Kargo Portal Bağlantısı — yalnızca http/https, boş bırakılabilir.</summary>
    public string? PortalUrl { get; set; }

    public string? Notes   { get; set; }
    public bool IsActive { get; set; }
    public Guid UpdatedByUserId { get; set; }
}
