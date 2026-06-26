namespace YonetimFinansalIslemTakipSistemi.Application.Features.Settings.MailSettings;

public class MailSettingsDto
{
    public string SmtpHost    { get; set; } = "";
    public int    SmtpPort    { get; set; } = 587;
    public bool   EnableSsl   { get; set; } = true;
    public string SenderEmail { get; set; } = "";
    public string SenderName  { get; set; } = "";
    public string Username    { get; set; } = "";
    /// <summary>UI'dan geldiğinde boşsa mevcut şifre korunur.</summary>
    public string Password    { get; set; } = "";
}
