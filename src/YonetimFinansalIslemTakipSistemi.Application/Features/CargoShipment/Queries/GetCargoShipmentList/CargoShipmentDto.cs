using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;

public class CargoShipmentDto
{
    public Guid Id { get; set; }
    public string? ShipmentNumber { get; set; }
    public CargoShipmentDirection Direction { get; set; }
    public DateTime ShipmentDate { get; set; }
    public TimeSpan? ShipmentTime { get; set; }
    public CargoShipmentType? ShipmentType { get; set; }
    public string? ShipmentTypeDisplay { get; set; }
    public Guid? CargoCompanyId { get; set; }
    public string? CargoCompanyName { get; set; }
    public Guid? CompanyDirectoryId { get; set; }
    public string? CompanyDirectoryName { get; set; }
    public string? SenderName   { get; set; }
    public string? ReceiverName { get; set; }
    public string? DeliveredBy  { get; set; }
    public string? ReceivedBy   { get; set; }
    public string? VehiclePlate { get; set; }
    public string? TrackingNumber { get; set; }
    public string? TrackingUrl    { get; set; }
    public CargoShipmentStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public CargoNotificationStatus NotificationStatus { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
