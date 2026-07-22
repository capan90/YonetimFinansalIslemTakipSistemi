using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification.MarkCargoNotificationPrepared;

public class MarkCargoNotificationPreparedRequest
{
    public Guid                   CargoShipmentId  { get; set; }
    public CargoShipmentDirection Direction        { get; set; }
    public NotificationType       NotificationType { get; set; }

    /// <summary>
    /// Opsiyonel: bildirimin gönderildiği alıcıların özeti (ör. "Murat (…), Ali (…)").
    /// Doluysa audit kaydına eklenir — toplu WhatsApp gönderiminde kim işlendi izlenir.
    /// </summary>
    public string? RecipientSummary { get; set; }
}
