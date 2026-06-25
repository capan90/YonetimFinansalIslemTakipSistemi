using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification.MarkCargoNotificationPrepared;

public class MarkCargoNotificationPreparedRequest
{
    public Guid                   CargoShipmentId  { get; set; }
    public CargoShipmentDirection Direction        { get; set; }
    public NotificationType       NotificationType { get; set; }
}
