using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;

public class GetCargoShipmentListQuery
{
    public CargoShipmentDirection Direction { get; set; }
    public string? Keyword    { get; set; }
    /// <summary>
    /// Arama türü: null/"Genel" = tüm alanlar, "Firma", "Kargo No", "Takip No", "Araç Plakası"
    /// </summary>
    public string? SearchType { get; set; }
    public CargoShipmentStatus? Status { get; set; }
    public CargoShipmentPriority? Priority { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo   { get; set; }
}
