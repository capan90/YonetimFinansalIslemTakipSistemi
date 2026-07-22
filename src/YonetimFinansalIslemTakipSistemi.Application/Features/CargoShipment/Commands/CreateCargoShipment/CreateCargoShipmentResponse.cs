using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.CreateCargoShipment;

public class CreateCargoShipmentResponse
{
    public Guid Id { get; set; }

    /// <summary>Otomatik üretilen numara — UI başarı mesajında kullanıcıya gösterilir.</summary>
    public string? ShipmentNumber { get; set; }

    public CargoShipmentDirection Direction { get; set; }
    public DateTime ShipmentDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
