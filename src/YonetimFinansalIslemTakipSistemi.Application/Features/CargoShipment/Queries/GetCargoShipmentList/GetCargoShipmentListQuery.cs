using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;

public class GetCargoShipmentListQuery
{
    public CargoShipmentDirection Direction { get; set; }
    public string? Keyword { get; set; }
    public CargoShipmentStatus? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo   { get; set; }
}
