using System.Text;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Services;

/// <summary>
/// WhatsApp mesaj şablonu üretir. Dış bağımlılığı yoktur; Application katmanında kalır.
/// Boş alanlar mesaja gereksiz satır eklemez.
/// </summary>
/// <remarks>
/// Mevcut akış: wa.me linki ile tarayıcı açılır, kullanıcı mesajı WhatsApp Web'den gönderir.
/// TODO: Gerçek programatik gönderim için WhatsApp Business Cloud API entegrasyonu gerekir
/// (graph.facebook.com/v17.0/{phone-number-id}/messages). WhatsApp Web otomatik gönderimi desteklemez.
/// Planlı sprint: WhatsApp Business Cloud API — bu sprinte dahil edilmedi.
/// </remarks>
public class WhatsAppNotificationComposer : INotificationComposer
{
    public NotificationType NotificationType => NotificationType.WhatsApp;

    public string Compose(CargoNotificationModel model)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Merhaba,");
        sb.AppendLine();
        sb.AppendLine("Tarafınıza gönderilen kargo bilgileri aşağıdadır.");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(model.ShipmentNumber))
            sb.AppendLine($"Kargo No: {model.ShipmentNumber}");

        if (!string.IsNullOrWhiteSpace(model.CargoCompany))
            sb.AppendLine($"Kargo Firması: {model.CargoCompany}");

        if (!string.IsNullOrWhiteSpace(model.TrackingNumber))
            sb.AppendLine($"Takip No: {model.TrackingNumber}");

        if (!string.IsNullOrWhiteSpace(model.TrackingUrl))
            sb.AppendLine($"Takip Linki: {model.TrackingUrl}");

        if (!string.IsNullOrWhiteSpace(model.Attention))
            sb.AppendLine($"Dikkatine: {model.Attention}");

        sb.AppendLine();
        sb.Append("Bilgilerinize.");

        return sb.ToString();
    }
}
