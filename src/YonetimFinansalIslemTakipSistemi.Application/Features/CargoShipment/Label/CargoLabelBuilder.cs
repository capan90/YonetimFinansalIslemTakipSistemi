using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Label;

/// <summary>
/// CargoShipment entity'sini CargoLabelModel'e dönüştürür.
/// UI hiçbir zaman entity'den doğrudan PDF üretmez; bu builder aracılık eder.
/// </summary>
public static class CargoLabelBuilder
{
    /// <summary>
    /// Snapshot alanlarından etiket modelini oluşturur.
    /// CompanyDirectory'den canlı veri okunmaz; entity'nin snapshot alanları kullanılır.
    /// CargoCompany navigasyon property'si yüklü olmalıdır (GetByIdWithIncludesAsync).
    /// </summary>
    public static CargoLabelModel Build(Domain.Entities.CargoShipment shipment)
    {
        return new CargoLabelModel
        {
            ShipmentNumber  = shipment.ShipmentNumber,

            // Alıcı bilgileri: snapshot — CompanyDirectory güncel verisi değil
            ReceiverCompany = shipment.ReceiverCompanyNameSnapshot,
            Attention       = shipment.ReceiverAttentionSnapshot,
            Address         = shipment.ReceiverAddressSnapshot,
            District        = shipment.ReceiverDistrictSnapshot,
            City            = shipment.ReceiverCitySnapshot,
            Phone           = shipment.ReceiverPhoneSnapshot,

            // Kargo firması adı: FK navigasyonu — CompanyDirectory değil
            CargoCompany    = shipment.CargoCompany?.Name,
            TrackingNumber  = shipment.TrackingNumber,

            Sender          = shipment.SenderName,
            VehiclePlate    = shipment.VehiclePlate,
            CreatedDate     = shipment.ShipmentDate,
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
