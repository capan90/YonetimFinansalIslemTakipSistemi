namespace YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.UpdateWhatsAppContact;

public class UpdateWhatsAppContactRequest
{
    public Guid    Id          { get; set; }
    public string  FullName    { get; set; } = string.Empty;
    public string  Phone       { get; set; } = string.Empty;
    public string? Company     { get; set; }
    public string? Description { get; set; }
    public bool    IsActive    { get; set; } = true;
}
