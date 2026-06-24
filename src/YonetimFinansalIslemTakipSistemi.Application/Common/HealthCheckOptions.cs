namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// HealthCheckService için gerekli ortam bilgileri.
/// UI katmanı (App.xaml.cs) bu nesneyi oluşturup DI'a kaydeder;
/// Infrastructure bu sınıfa doğrudan bağımlı değildir.
/// </summary>
public sealed record HealthCheckOptions
{
    public string AppEnvironment    { get; init; } = "Development";
    public string LogDirectory      { get; init; } = "";
    public string BackupDirectory   { get; init; } = "";
    public string UpdatePublishPath { get; init; } = "";
    public string ConnectionString  { get; init; } = "";

    // Bildirim durumu — Health Check ekranına gösterilir
    public bool   NotificationsEnabled             { get; init; }
    public string NotificationProvider             { get; init; } = "";
    public bool   NotificationToConfigured         { get; init; }
    public string NotificationSmtpHost            { get; init; } = "";
    public bool   NotificationCredentialsConfigured { get; init; }
}
