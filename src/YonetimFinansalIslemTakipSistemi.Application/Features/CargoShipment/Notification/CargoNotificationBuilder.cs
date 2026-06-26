using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification;

/// <summary>
/// CargoShipment entity'sini CargoNotificationModel'e dönüştürür.
/// Etiket mimarisiyle tutarlı: alıcı verisi snapshot'tan, CargoCompany navigasyon property'sinden.
/// CompanyDirectory canlı verisi okunmaz.
/// </summary>
public static class CargoNotificationBuilder
{
    public static CargoNotificationModel Build(Domain.Entities.CargoShipment shipment)
    {
        return new CargoNotificationModel
        {
            ShipmentId      = shipment.Id,
            ShipmentNumber  = shipment.ShipmentNumber,

            // Alıcı: snapshot — oluşturma anındaki firma bilgileri
            ReceiverCompany = shipment.ReceiverCompanyNameSnapshot,
            Attention       = shipment.ReceiverAttentionSnapshot,
            TargetPhone     = shipment.ReceiverPhoneSnapshot,
            TargetEmail     = shipment.ReceiverEmailSnapshot,

            // Kargo firması: navigasyon (etiket mimarisiyle tutarlı)
            CargoCompany    = shipment.CargoCompany?.Name,
            TrackingNumber  = shipment.TrackingNumber,
            TrackingUrl     = shipment.TrackingUrl,

            Sender          = shipment.SenderName,
            ShipmentDate    = shipment.ShipmentDate,
            VehiclePlate    = shipment.VehiclePlate,
            Priority        = DisplayPriority(shipment.Priority)
        };
    }

    private static string DisplayPriority(CargoShipmentPriority p) => p switch
    {
        CargoShipmentPriority.Medium   => "Orta",
        CargoShipmentPriority.Urgent   => "Acil",
        CargoShipmentPriority.Critical => "Çok Acil",
        _                              => "Normal"
    };
}
