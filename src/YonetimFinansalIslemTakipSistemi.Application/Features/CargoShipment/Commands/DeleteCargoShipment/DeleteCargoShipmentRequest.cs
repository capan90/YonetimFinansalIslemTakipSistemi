using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.DeleteCargoShipment;

public class DeleteCargoShipmentRequest
{
    public Guid Id { get; set; }
    public CargoShipmentDirection Direction { get; set; }
    public Guid DeletedByUserId { get; set; }
}
