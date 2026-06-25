using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification.GenerateCargoNotification;

public class GenerateCargoNotificationRequest
{
    public Guid                    CargoShipmentId  { get; set; }
    public CargoShipmentDirection  Direction        { get; set; }
    public NotificationType        NotificationType { get; set; }
}
