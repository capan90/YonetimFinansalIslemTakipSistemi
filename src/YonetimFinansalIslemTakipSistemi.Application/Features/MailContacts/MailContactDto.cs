namespace YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts;

public class MailContactDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;

    /// <summary>Normalize (küçük harf) e-posta adresi.</summary>
    public string Email { get; set; } = string.Empty;

    public string?   Company     { get; set; }
    public string?   Description { get; set; }
    public bool      IsDefaultCc { get; set; }
    public DateTime? LastUsedAt  { get; set; }
    public bool      IsActive    { get; set; }
    public DateTime  CreatedAt   { get; set; }

    /// <summary>Liste ve chip gösterimi: "Ad (Firma)" veya yalnızca ad.</summary>
    public string DisplayText => string.IsNullOrWhiteSpace(Company) ? FullName : $"{FullName} ({Company})";

    /// <summary>Açılır listede adresi de gösteren uzun etiket.</summary>
    public string DisplayWithEmail => $"{DisplayText} — {Email}";
}
