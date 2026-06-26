namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// Kargo bildirim sisteminin gönderici yapılandırması.
/// FromEmail ileride Company Settings modülüne taşınabilir.
/// </summary>
public sealed record CargoNotificationOptions
{
    // Kargo bilgilendirme maillerinde gönderici adresi
    public string FromEmail { get; init; } = "";
}
