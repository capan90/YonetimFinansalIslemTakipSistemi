using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoReport;

public class CargoReportRowDto
{
    public Guid                   Id                         { get; set; }
    public string?                ShipmentNumber             { get; set; }
    public CargoShipmentDirection Direction                  { get; set; }
    public string                 DirectionDisplay           { get; set; } = "";
    public DateTime               ShipmentDate               { get; set; }
    public string?                Party                      { get; set; }
    public string?                CargoCompanyName           { get; set; }
    public string?                TrackingNumber             { get; set; }
    public string?                VehiclePlate               { get; set; }
    public string                 StatusDisplay              { get; set; } = "";
    public string                 NotificationStatusDisplay  { get; set; } = "";
    public string                 PriorityDisplay            { get; set; } = "";
}
