namespace YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts;

public class WhatsAppContactDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;

    /// <summary>Normalize format: +905321234567</summary>
    public string Phone { get; set; } = string.Empty;

    public string? Company     { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Liste ve chip gösterimi: "Ad (Firma)" veya yalnızca ad.</summary>
    public string DisplayText => string.IsNullOrWhiteSpace(Company) ? FullName : $"{FullName} ({Company})";
}
