using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.CreateCargoShipment;

public class CreateCargoShipmentRequest
{
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
    public CargoShipmentStatus Status { get; set; } = CargoShipmentStatus.Draft;
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
}
