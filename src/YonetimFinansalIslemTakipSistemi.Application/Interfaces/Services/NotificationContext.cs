namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Hata bildirimine ek bağlam — opsiyonel; null geçilebilir.
/// </summary>
public sealed class NotificationContext
{
    public string? UserId   { get; init; }
    public string? UserName { get; init; }
    public string? Screen   { get; init; }
    public string  Level    { get; init; } = "Error";
}
