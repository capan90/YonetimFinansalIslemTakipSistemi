using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Kargo durum geçiş kuralları — gelen ve giden kargo ayrı durum makineleri kullanır.
/// Sprint 17 regresyonu: yönsüz IsAllowed gelen kargonun geçerli geçişlerini reddediyordu.
/// </summary>
public class CargoStatusTransitionTests
{
    // ── Giden kargo ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(CargoShipmentStatus.Prepared,      CargoShipmentStatus.HandedToCargo)]
    [InlineData(CargoShipmentStatus.Prepared,      CargoShipmentStatus.Delivered)]
    [InlineData(CargoShipmentStatus.HandedToCargo, CargoShipmentStatus.Shipped)]
    [InlineData(CargoShipmentStatus.Shipped,       CargoShipmentStatus.Delivered)]
    [InlineData(CargoShipmentStatus.Shipped,       CargoShipmentStatus.Cancelled)]
    public void Giden_GecerliGecisler_KabulEdilir(CargoShipmentStatus from, CargoShipmentStatus to)
        => Assert.True(CargoStatusTransitions.IsAllowed(from, to, CargoShipmentDirection.Outgoing));

    [Theory]
    [InlineData(CargoShipmentStatus.Delivered, CargoShipmentStatus.Prepared)]
    [InlineData(CargoShipmentStatus.Delivered, CargoShipmentStatus.Shipped)]
    [InlineData(CargoShipmentStatus.Cancelled, CargoShipmentStatus.Prepared)]
    [InlineData(CargoShipmentStatus.Shipped,   CargoShipmentStatus.Prepared)]
    public void Giden_GecersizGecisler_Reddedilir(CargoShipmentStatus from, CargoShipmentStatus to)
        => Assert.False(CargoStatusTransitions.IsAllowed(from, to, CargoShipmentDirection.Outgoing));

    // ── Gelen kargo ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(CargoShipmentStatus.Waiting,  CargoShipmentStatus.Received)]
    [InlineData(CargoShipmentStatus.Waiting,  CargoShipmentStatus.Cancelled)]
    [InlineData(CargoShipmentStatus.Received, CargoShipmentStatus.PersonnelDelivered)]
    [InlineData(CargoShipmentStatus.Received, CargoShipmentStatus.Cancelled)]
    public void Gelen_GecerliGecisler_KabulEdilir(CargoShipmentStatus from, CargoShipmentStatus to)
        => Assert.True(CargoStatusTransitions.IsAllowed(from, to, CargoShipmentDirection.Incoming));

    [Theory]
    [InlineData(CargoShipmentStatus.PersonnelDelivered, CargoShipmentStatus.Waiting)]
    [InlineData(CargoShipmentStatus.Cancelled,          CargoShipmentStatus.Received)]
    [InlineData(CargoShipmentStatus.Received,           CargoShipmentStatus.Waiting)]
    public void Gelen_GecersizGecisler_Reddedilir(CargoShipmentStatus from, CargoShipmentStatus to)
        => Assert.False(CargoStatusTransitions.IsAllowed(from, to, CargoShipmentDirection.Incoming));

    // ── Ortak kurallar ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(CargoShipmentDirection.Incoming)]
    [InlineData(CargoShipmentDirection.Outgoing)]
    public void AyniDurumaKalmak_HerZamanGecerli(CargoShipmentDirection direction)
    {
        foreach (var status in Enum.GetValues<CargoShipmentStatus>())
            Assert.True(CargoStatusTransitions.IsAllowed(status, status, direction));
    }

    [Fact]
    public void GelenWaitingReceived_GidenKuralindaGecersizdir_YonAyrimiKorunur()
    {
        // Regresyon: yön ayrımı kaldırılırsa bu iki assert birlikte tutmaz
        Assert.True(CargoStatusTransitions.IsAllowed(
            CargoShipmentStatus.Waiting, CargoShipmentStatus.Received, CargoShipmentDirection.Incoming));
        Assert.False(CargoStatusTransitions.IsAllowed(
            CargoShipmentStatus.Waiting, CargoShipmentStatus.Received, CargoShipmentDirection.Outgoing));
    }

    [Fact]
    public void GetAllowedNext_MevcutDurumuDaIcerir()
    {
        var next = CargoStatusTransitions.GetAllowedNext(CargoShipmentStatus.Waiting, CargoShipmentDirection.Incoming);

        Assert.Contains(CargoShipmentStatus.Waiting,  next);
        Assert.Contains(CargoShipmentStatus.Received, next);
    }

    [Fact]
    public void GetAllowedNext_IsAllowed_IleTutarlidir()
    {
        // UI'ın sunduğu her seçenek handler doğrulamasından da geçmelidir
        foreach (var direction in Enum.GetValues<CargoShipmentDirection>())
        foreach (var status in Enum.GetValues<CargoShipmentStatus>())
        foreach (var next in CargoStatusTransitions.GetAllowedNext(status, direction))
            Assert.True(CargoStatusTransitions.IsAllowed(status, next, direction),
                $"UI {direction} için {status}→{next} sunuyor ama handler reddediyor.");
    }
}
