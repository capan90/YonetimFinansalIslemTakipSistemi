namespace YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts.CreateMailContact;

public class CreateMailContactRequest
{
    public string  FullName    { get; set; } = string.Empty;
    public string  Email       { get; set; } = string.Empty;
    public string? Company     { get; set; }
    public string? Description { get; set; }

    /// <summary>true ise mail hazırlama ekranında CC alanına otomatik eklenir.</summary>
    public bool    IsDefaultCc { get; set; }
}
