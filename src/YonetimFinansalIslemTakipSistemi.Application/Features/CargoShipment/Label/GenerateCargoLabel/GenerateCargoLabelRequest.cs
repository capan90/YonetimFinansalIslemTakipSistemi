using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Label.GenerateCargoLabel;

public class GenerateCargoLabelRequest
{
    public Guid Id { get; set; }
    public CargoShipmentDirection Direction { get; set; }
}
