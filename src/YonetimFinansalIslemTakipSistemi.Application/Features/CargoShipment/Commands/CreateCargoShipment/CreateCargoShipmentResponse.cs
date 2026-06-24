using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.CreateCargoShipment;

public class CreateCargoShipmentResponse
{
    public Guid Id { get; set; }
    public CargoShipmentDirection Direction { get; set; }
    public DateTime ShipmentDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
