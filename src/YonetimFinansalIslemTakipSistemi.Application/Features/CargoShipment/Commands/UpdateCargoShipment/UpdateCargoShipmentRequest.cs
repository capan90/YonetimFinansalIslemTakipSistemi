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
    public Guid? CargoCompanyId { get; set; }
    public Guid? CompanyDirectoryId { get; set; }
    public string? SenderName   { get; set; }
    public string? ReceiverName { get; set; }
    public string? DeliveredBy  { get; set; }
    public string? ReceivedBy   { get; set; }
    public string? VehiclePlate { get; set; }
    public string? TrackingNumber { get; set; }
    public CargoShipmentStatus Status { get; set; }
    public CargoNotificationStatus NotificationStatus { get; set; }
    public string? Notes { get; set; }
    public Guid UpdatedByUserId { get; set; }
}
