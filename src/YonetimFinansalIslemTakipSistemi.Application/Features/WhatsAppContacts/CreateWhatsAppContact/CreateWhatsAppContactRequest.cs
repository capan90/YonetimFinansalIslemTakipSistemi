namespace YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.CreateWhatsAppContact;

public class CreateWhatsAppContactRequest
{
    public string  FullName    { get; set; } = string.Empty;
    public string  Phone       { get; set; } = string.Empty;
    public string? Company     { get; set; }
    public string? Description { get; set; }
}
