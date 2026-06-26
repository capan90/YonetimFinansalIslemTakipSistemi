using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoReport;

public class GetCargoReportQuery
{
    public DateTime?                DateFrom           { get; set; }
    public DateTime?                DateTo             { get; set; }
    public CargoShipmentDirection?  Direction          { get; set; }
    /// <summary>Firma rehberi / gönderici / alıcı adında serbest metin arama.</summary>
    public string?                  Keyword            { get; set; }
    public Guid?                    CargoCompanyId     { get; set; }
    /// <summary>PDF filtre özetinde gösterilmek üzere seçilen firma adı.</summary>
    public string?                  CargoCompanyName   { get; set; }
    public CargoShipmentStatus?     Status             { get; set; }
    public CargoNotificationStatus? NotificationStatus { get; set; }
    public CargoShipmentPriority?   Priority           { get; set; }
    public string?                  VehiclePlate       { get; set; }
    public string?                  TrackingNumber     { get; set; }
    public string?                  ShipmentNumber     { get; set; }
}
