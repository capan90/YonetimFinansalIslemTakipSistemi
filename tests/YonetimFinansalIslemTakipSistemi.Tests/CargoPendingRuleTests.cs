using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// "Bekleyen" tanımı Dashboard kartı ve kargo raporunda ortaktır.
/// İki ekranın farklı sayı göstermesi kullanıcı güvenini bozar; kural tek kaynaktır.
/// </summary>
public class CargoPendingRuleTests
{
    [Theory]
    [InlineData(CargoShipmentStatus.Draft)]
    [InlineData(CargoShipmentStatus.Prepared)]
    [InlineData(CargoShipmentStatus.HandedToCargo)]
    [InlineData(CargoShipmentStatus.Shipped)]
    [InlineData(CargoShipmentStatus.Waiting)]
    [InlineData(CargoShipmentStatus.Received)]
    public void SureciDevamEdenDurumlar_BekleyenSayilir(CargoShipmentStatus status)
        => Assert.True(CargoShipmentStatusRules.IsPending(status));

    [Theory]
    [InlineData(CargoShipmentStatus.Delivered)]
    [InlineData(CargoShipmentStatus.Cancelled)]
    // Gelen kargoda personele teslim, kaydın operasyonel olarak kapandığı andır
    [InlineData(CargoShipmentStatus.PersonnelDelivered)]
    public void KapanmisDurumlar_BekleyenSayilmaz(CargoShipmentStatus status)
        => Assert.False(CargoShipmentStatusRules.IsPending(status));

    [Fact]
    public void TumDurumlar_KuralaGoreIkiyeAyrilir_YeniDurumUnutulmaz()
    {
        // Enum'a yeni durum eklendiğinde bu test onu görünür kılar:
        // her durum ya bekleyen ya kapanmış olmalıdır, ara değer yoktur.
        var all      = Enum.GetValues<CargoShipmentStatus>();
        var pending  = all.Count(CargoShipmentStatusRules.IsPending);
        var closed   = all.Count(s => !CargoShipmentStatusRules.IsPending(s));

        Assert.Equal(all.Length, pending + closed);
        Assert.Equal(3, closed);   // Delivered, Cancelled, PersonnelDelivered
    }
}
