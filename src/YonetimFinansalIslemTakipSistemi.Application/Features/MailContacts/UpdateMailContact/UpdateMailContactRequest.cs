namespace YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts.UpdateMailContact;

public class UpdateMailContactRequest
{
    public Guid    Id          { get; set; }
    public string  FullName    { get; set; } = string.Empty;
    public string  Email       { get; set; } = string.Empty;
    public string? Company     { get; set; }
    public string? Description { get; set; }
    public bool    IsDefaultCc { get; set; }
    public bool    IsActive    { get; set; } = true;
}
