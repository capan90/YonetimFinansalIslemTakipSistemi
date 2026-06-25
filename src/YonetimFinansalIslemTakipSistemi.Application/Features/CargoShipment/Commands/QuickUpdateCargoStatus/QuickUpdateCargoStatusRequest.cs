using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.QuickUpdateCargoStatus;

/// <summary>
/// Tam edit formu açılmadan kargo durumunu hızlıca değiştirmek için.
/// Sadece Status alanını günceller; diğer alanlar ve snapshot korunur.
/// </summary>
public class QuickUpdateCargoStatusRequest
{
    public Guid Id { get; set; }
    public CargoShipmentDirection Direction { get; set; }
    public CargoShipmentStatus NewStatus { get; set; }
    public Guid UpdatedByUserId { get; set; }
}
