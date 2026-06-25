using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.CreateCargoShipment;

public class CreateCargoShipmentRequest
{
    public string? ShipmentNumber { get; set; }
    public CargoShipmentDirection Direction { get; set; }
    public DateTime ShipmentDate { get; set; }
    public TimeSpan? ShipmentTime { get; set; }
    public CargoShipmentType? ShipmentType { get; set; }
    public CargoShipmentPriority Priority { get; set; } = CargoShipmentPriority.Normal;
    public CargoShipmentCreatedFrom CreatedFrom { get; set; } = CargoShipmentCreatedFrom.Manual;
    public Guid? CargoCompanyId { get; set; }
    public Guid? CompanyDirectoryId { get; set; }

    // Firma rehberinden kargo oluşturulurken snapshot alınır; adres değişse bile kargo kaydı korunur
    public string? ReceiverCompanyNameSnapshot { get; set; }
    public string? ReceiverAddressSnapshot     { get; set; }
    public string? ReceiverAttentionSnapshot   { get; set; }
    public string? ReceiverCitySnapshot        { get; set; }
    public string? ReceiverDistrictSnapshot    { get; set; }
    public string? ReceiverPhoneSnapshot       { get; set; }
    public string? ReceiverEmailSnapshot       { get; set; }

    public string? SenderName   { get; set; }
    public string? ReceiverName { get; set; }
    public string? DeliveredBy  { get; set; }
    public string? ReceivedBy   { get; set; }
    public string? VehiclePlate { get; set; }
    public string? TrackingNumber { get; set; }
    /// <summary>Manuel girilmişse kullanılır; boşsa handler şablondan üretir.</summary>
    public string? TrackingUrl { get; set; }
    public CargoShipmentStatus Status { get; set; } = CargoShipmentStatus.Draft;
    public string? Notes { get; set; }
    public Guid CreatedByUserId { get; set; }
}
