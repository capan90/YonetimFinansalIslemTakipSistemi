namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Label;

/// <summary>
/// Kargo etiketinde basılacak verilerin katmandan bağımsız modeli.
/// Alıcı verisi: CargoShipment snapshot alanları (CompanyDirectory'den canlı veri okunmaz).
/// Gönderici verisi: ICompanyInfoProvider (AppSettings → ileride Company Settings modülü).
/// </summary>
public class CargoLabelModel
{
    public string? ShipmentNumber { get; set; }

    // ── Alıcı — snapshot'tan doldurulur ─────────────────────────────────
    public string? ReceiverCompany { get; set; }
    public string? Attention       { get; set; }
    public string? Address         { get; set; }
    public string? District        { get; set; }
    public string? City            { get; set; }
    public string? Phone           { get; set; }

    // ── Kargo firması — CargoShipment.CargoCompany navigasyonu ──────────
    public string? CargoCompany   { get; set; }
    public string? TrackingNumber { get; set; }

    // ── Operasyonel ─────────────────────────────────────────────────────
    public string?   Sender       { get; set; }
    public string?   VehiclePlate { get; set; }
    public DateTime  CreatedDate  { get; set; }
    public string    Priority     { get; set; } = "Normal";

    // ── Gönderici firma (ICompanyInfoProvider) ───────────────────────────
    public string? SenderCompanyName     { get; set; }
    public string? SenderCompanyAddress  { get; set; }
    public string? SenderCompanyDistrict { get; set; }
    public string? SenderCompanyCity     { get; set; }
    public string? SenderCompanyPhone    { get; set; }
    public string? SenderLogoPath        { get; set; }
}
