using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Bildirim kanalına özgü mesaj metni üretir.
/// V1: WhatsAppNotificationComposer.
/// İleride MailNotificationComposer aynı interface'i uygular; handler değişmez.
/// </summary>
public interface INotificationComposer
{
    NotificationType NotificationType { get; }

    string Compose(CargoNotificationModel model);
}
