using System.Text;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Services;

/// <summary>
/// Kurumsal Türkçe mail şablonu üretir. Dış bağımlılığı yoktur; Application katmanında kalır.
/// SMTP gönderimi bu sınıfın sorumluluğu değildir — yalnızca body ve subject hazırlar.
/// </summary>
public class MailNotificationComposer : INotificationComposer
{
    public NotificationType NotificationType => NotificationType.Mail;

    /// <summary>Mail konusu: "Kargo Bilgilendirme - G-2026-0001"</summary>
    public string ComposeSubject(CargoNotificationModel model)
        => $"Kargo Bilgilendirme - {model.ShipmentNumber}";

    public string Compose(CargoNotificationModel model)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Merhaba,");
        sb.AppendLine();
        sb.AppendLine("Tarafınıza ait kargo bilgileri aşağıdadır.");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(model.ShipmentNumber))
            sb.AppendLine($"Kargo No: {model.ShipmentNumber}");

        if (!string.IsNullOrWhiteSpace(model.CargoCompany))
            sb.AppendLine($"Kargo Firması: {model.CargoCompany}");

        if (!string.IsNullOrWhiteSpace(model.TrackingNumber))
            sb.AppendLine($"Takip No: {model.TrackingNumber}");

        if (!string.IsNullOrWhiteSpace(model.TrackingUrl))
            sb.AppendLine($"Takip Linki: {model.TrackingUrl}");

        var attentionLine = AttentionHelper.FormatAttentionLine(model.Attention);
        if (attentionLine != "Muhattap: -")
            sb.AppendLine(attentionLine);

        sb.AppendLine();
        sb.Append("Bilgilerinize sunar, iyi çalışmalar dileriz.");

        return sb.ToString();
    }
}
