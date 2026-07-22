using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

public class CargoNumberFormatterTests
{
    [Fact]
    public void IlkGelen_GLN00001()
        => Assert.Equal("GLN00001", CargoNumberFormatter.Format(CargoShipmentDirection.Incoming, 1));

    [Fact]
    public void IlkGiden_GDN00001()
        => Assert.Equal("GDN00001", CargoNumberFormatter.Format(CargoShipmentDirection.Outgoing, 1));

    [Fact]
    public void BesHaneninUzerinde_HaneSayisiBuyur()
        => Assert.Equal("GDN123456", CargoNumberFormatter.Format(CargoShipmentDirection.Outgoing, 123456));

    [Theory]
    [InlineData(CargoShipmentDirection.Incoming, "GLN")]
    [InlineData(CargoShipmentDirection.Outgoing, "GDN")]
    public void Prefix_YoneGoreDogru(CargoShipmentDirection direction, string expected)
        => Assert.Equal(expected, CargoNumberFormatter.Prefix(direction));
}
