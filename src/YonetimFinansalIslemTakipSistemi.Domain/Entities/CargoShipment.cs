using YonetimFinansalIslemTakipSistemi.Domain.Common;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Domain.Entities;

/// <summary>
/// Gelen ve giden kargo kaydı. Direction alanı ile ayırt edilir.
/// </summary>
public class CargoShipment : BaseEntity
{
    /// <summary>Referans/iç kargo numarası. Otomatik veya manuel girilebilir.</summary>
    public string? ShipmentNumber { get; set; }

    public CargoShipmentDirection Direction { get; set; }
    public DateTime ShipmentDate { get; set; }
    public TimeSpan? ShipmentTime { get; set; }
    public CargoShipmentType? ShipmentType { get; set; }

    /// <summary>Operasyonel öncelik. Listelemede renk kodu ve Dashboard filtresi için kullanılır.</summary>
    public CargoShipmentPriority Priority { get; set; } = CargoShipmentPriority.Normal;

    /// <summary>Kaydın hangi kanaldan oluşturulduğunu belirtir. Şimdilik hep Manual; ileride ExcelImport/Api genişler.</summary>
    public CargoShipmentCreatedFrom CreatedFrom { get; set; } = CargoShipmentCreatedFrom.Manual;

    /// <summary>Kargo firması opsiyonel — gelen kargoda firma bilinmeyebilir.</summary>
    public Guid? CargoCompanyId { get; set; }
    public CargoCompany? CargoCompany { get; set; }

    /// <summary>Firma rehberinden seçilen karşı taraf. FK referans için tutulur; operasyon verisi Snapshot'ta.</summary>
    public Guid? CompanyDirectoryId { get; set; }
    public CompanyDirectory? CompanyDirectory { get; set; }

    // ── Alıcı Firma Snapshot Alanları ──────────────────────────────────────────
    // Kargo oluşturulurken CompanyDirectory'den bir kez kopyalanır.
    // Sonradan firma adresi değişse bile kargo kaydı oluşturma anındaki verileri korur.
    // Etiket, WhatsApp ve mail şablonları bu alanları kullanır.

    public string? ReceiverCompanyNameSnapshot { get; set; }
    public string? ReceiverAddressSnapshot     { get; set; }
    public string? ReceiverAttentionSnapshot   { get; set; }
    public string? ReceiverCitySnapshot        { get; set; }
    public string? ReceiverDistrictSnapshot    { get; set; }
    public string? ReceiverPhoneSnapshot       { get; set; }
    public string? ReceiverEmailSnapshot       { get; set; }

    // ── Operasyon Alanları ────────────────────────────────────────────────────

    public string? SenderName   { get; set; }
    public string? ReceiverName { get; set; }

    /// <summary>Giden kargoda teslim eden kişi/kurye adı.</summary>
    public string? DeliveredBy { get; set; }

    /// <summary>Gelen kargoda teslim alan kişi adı.</summary>
    public string? ReceivedBy { get; set; }

    public string? VehiclePlate { get; set; }

    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Hesaplanan takip URL'i: CargoCompany.TrackingUrlTemplate + TrackingNumber.
    /// Takip numarası veya şablon yoksa boş kalır.
    /// </summary>
    public string? TrackingUrl { get; set; }

    public CargoShipmentStatus Status { get; set; } = CargoShipmentStatus.Draft;

    public CargoNotificationStatus NotificationStatus { get; set; } = CargoNotificationStatus.NotNotified;

    public string? Notes { get; set; }
}
