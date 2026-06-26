using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.UpdateCargoShipment;

public class UpdateCargoShipmentRequest
{
    public Guid Id { get; set; }
    public string? ShipmentNumber { get; set; }
    public CargoShipmentDirection Direction { get; set; }
    public DateTime ShipmentDate { get; set; }
    public TimeSpan? ShipmentTime { get; set; }
    public CargoShipmentType? ShipmentType { get; set; }
    public CargoShipmentPriority Priority { get; set; }
    public Guid? CargoCompanyId { get; set; }
    public Guid? CompanyDirectoryId { get; set; }
    public string? SenderName   { get; set; }
    public string? ReceiverName { get; set; }
    public string? DeliveredBy  { get; set; }
    public string? ReceivedBy   { get; set; }
    public string? VehiclePlate { get; set; }
    public string? TrackingNumber { get; set; }
    /// <summary>Manuel girilmişse kullanılır; boşsa handler şablondan üretir.</summary>
    public string? TrackingUrl { get; set; }
    public CargoShipmentStatus Status { get; set; }
    public CargoNotificationStatus NotificationStatus { get; set; }
    public string? Notes { get; set; }
    public Guid UpdatedByUserId { get; set; }

    // Firma snapshot güncelleme: varsayılan false.
    // Yalnızca kullanıcı "Firma Bilgilerini Yenile" butonuna bastığında true olur.
    public bool    UpdateSnapshot      { get; set; }
    public string? SnapshotCompanyName { get; set; }
    public string? SnapshotAddress     { get; set; }
    public string? SnapshotAttention   { get; set; }
    public string? SnapshotCity        { get; set; }
    public string? SnapshotDistrict    { get; set; }
    public string? SnapshotPhone       { get; set; }
    public string? SnapshotEmail       { get; set; }
}
