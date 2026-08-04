using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;

/// <summary>
/// Kargo durumuyla ilgili iş kuralları — Dashboard kartı ve raporun aynı tanımı
/// kullanması için tek kaynak. İki ekranın "Bekleyen" için farklı sayı göstermesi
/// kullanıcı güvenini bozar; sayım mantığı buraya toplanmıştır.
/// </summary>
public static class CargoShipmentStatusRules
{
    /// <summary>
    /// İş kuralı: "Bekleyen" = süreci fiilen kapanmamış kargo.
    /// Kapanmış sayılan durumlar: Teslim Edildi, İptal ve — gelen kargoda kaydın
    /// operasyonel olarak sonlandığı — Personele Teslim Edildi.
    /// </summary>
    public static bool IsPending(CargoShipmentStatus status) =>
        status != CargoShipmentStatus.Delivered &&
        status != CargoShipmentStatus.Cancelled &&
        status != CargoShipmentStatus.PersonnelDelivered;

    /// <summary>Kartlarda/raporlarda gösterilecek ortak açıklama metni.</summary>
    public const string PendingDescription =
        "Teslim Edildi, Personele Teslim Edildi ve İptal dışındaki tüm kargolar " +
        "(gelen + giden, tarih filtresi uygulanmaz).";
}
