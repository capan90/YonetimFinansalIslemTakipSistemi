namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification;

/// <summary>
/// Bildirim kanalından bağımsız kargo mesaj modeli.
/// Alıcı verisi snapshot'tan; gönderici verisi handler'a bağlıdır.
/// MessageBody ve TargetPhone INotificationComposer tarafından doldurulur.
/// </summary>
public class CargoNotificationModel
{
    public Guid    ShipmentId     { get; set; }
    public string? ShipmentNumber { get; set; }

    // ── Alıcı — snapshot'tan (CompanyDirectory canlı verisi değil) ──────
    public string? ReceiverCompany { get; set; }
    public string? Attention       { get; set; }
    public string? TargetPhone     { get; set; }

    // ── Kargo operasyon ─────────────────────────────────────────────────
    public string?   CargoCompany   { get; set; }
    public string?   TrackingNumber { get; set; }
    public string?   TrackingUrl    { get; set; }
    public string?   Sender         { get; set; }
    public DateTime  ShipmentDate   { get; set; }
    public string?   VehiclePlate   { get; set; }
    public string    Priority       { get; set; } = "Normal";

    // ── Composer tarafından doldurulur ───────────────────────────────────
    public string  MessageBody  { get; set; } = string.Empty;

    // ── Mail kanalı alanları (MailNotificationComposer doldurur) ─────────
    public string? TargetEmail  { get; set; }
    public string? Subject      { get; set; }
}
