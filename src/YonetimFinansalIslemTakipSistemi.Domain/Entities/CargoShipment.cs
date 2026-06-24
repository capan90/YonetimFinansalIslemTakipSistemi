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

    /// <summary>Gelen mi giden mi olduğunu belirtir.</summary>
    public CargoShipmentDirection Direction { get; set; }

    public DateTime ShipmentDate { get; set; }

    /// <summary>Kargo saati. Opsiyonel; girilmezse null bırakılır.</summary>
    public TimeSpan? ShipmentTime { get; set; }

    public CargoShipmentType? ShipmentType { get; set; }

    /// <summary>Kargo firması opsiyonel — gelen kargoda firma bilinmeyebilir.</summary>
    public Guid? CargoCompanyId { get; set; }
    public CargoCompany? CargoCompany { get; set; }

    /// <summary>Firma rehberinden seçilen karşı taraf. Opsiyonel; manuel giriş de yapılabilir.</summary>
    public Guid? CompanyDirectoryId { get; set; }
    public CompanyDirectory? CompanyDirectory { get; set; }

    public string? SenderName   { get; set; }
    public string? ReceiverName { get; set; }

    /// <summary>Giden kargoda teslim eden kişi/kurye adı.</summary>
    public string? DeliveredBy { get; set; }

    /// <summary>Gelen kargoda teslim alan kişi adı.</summary>
    public string? ReceivedBy { get; set; }

    public string? VehiclePlate { get; set; }

    /// <summary>Manuel girilen takip numarası.</summary>
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
