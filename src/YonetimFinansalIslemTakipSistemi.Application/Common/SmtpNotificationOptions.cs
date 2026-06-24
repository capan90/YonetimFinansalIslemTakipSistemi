namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// SmtpErrorNotificationService için yapılandırma.
/// App.xaml.cs tarafından appsettings.json + env var'lardan oluşturulur ve DI'a kaydedilir.
/// Şifre env var YONETIM_SMTP_PASSWORD ile override edilebilir.
/// </summary>
public sealed record SmtpNotificationOptions
{
    public bool   Enabled       { get; init; }
    public string Provider      { get; init; } = "Smtp";
    public string MinimumLevel  { get; init; } = "Error";
    public string To            { get; init; } = "";
    public string From          { get; init; } = "";
    public string SmtpHost      { get; init; } = "";
    public int    SmtpPort      { get; init; } = 587;
    public bool   SmtpEnableSsl { get; init; } = true;
    public string SmtpUsername  { get; init; } = "";
    public string SmtpPassword  { get; init; } = "";
}
