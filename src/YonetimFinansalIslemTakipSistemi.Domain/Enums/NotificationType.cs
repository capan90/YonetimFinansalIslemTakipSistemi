namespace YonetimFinansalIslemTakipSistemi.Domain.Enums;

/// <summary>
/// Bildirim kanalı. V1'de yalnızca WhatsApp aktif; Mail mimarisi hazır.
/// </summary>
public enum NotificationType
{
    WhatsApp = 1,
    Mail     = 2
}
