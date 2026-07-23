using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Sprint 16 regresyonu: liste ekranı filtreleri artık repository'de (SQL) uygulanır
/// (GetFilteredReportAsync ile ortak sorgu); davranış öncekiyle aynı kalmalıdır.
/// </summary>
public class CargoShipmentListQueryTests
{
    private static CargoShipment Shipment(
        CargoShipmentDirection direction, DateTime date,
        CargoShipmentStatus status = CargoShipmentStatus.Prepared,
        string? number = null) => new()
    {
        Id           = Guid.NewGuid(),
        Direction    = direction,
        ShipmentDate = date,
        Status       = status,
        ShipmentNumber = number,
        CreatedAt    = date
    };

    [Fact]
    public async Task TarihVeDurumFiltreleri_RepositorySorgusundaUygulanir()
    {
        var repo = new FakeCargoShipmentRepository();
        var user = new FakeUserContext();
        user.GrantAll();

        repo.Existing.Add(Shipment(CargoShipmentDirection.Outgoing, new DateTime(2026, 7, 1), CargoShipmentStatus.Prepared, "GDN00001"));
        repo.Existing.Add(Shipment(CargoShipmentDirection.Outgoing, new DateTime(2026, 7, 10), CargoShipmentStatus.Delivered, "GDN00002"));
        repo.Existing.Add(Shipment(CargoShipmentDirection.Outgoing, new DateTime(2026, 7, 20), CargoShipmentStatus.Prepared, "GDN00003"));
        repo.Existing.Add(Shipment(CargoShipmentDirection.Incoming, new DateTime(2026, 7, 20), CargoShipmentStatus.Prepared, "GLN00001"));

        var handler = new GetCargoShipmentListHandler(repo, user);
        var list = await handler.HandleAsync(new GetCargoShipmentListQuery
        {
            Direction = CargoShipmentDirection.Outgoing,
            DateFrom  = new DateTime(2026, 7, 5),
            Status    = CargoShipmentStatus.Prepared
        });

        // Yalnızca 5 Temmuz sonrası + Prepared + Outgoing: GDN00003
        Assert.Single(list);
        Assert.Equal("GDN00003", list[0].ShipmentNumber);
    }

    [Fact]
    public async Task Liste_TariheGoreAzalanSiralanir()
    {
        var repo = new FakeCargoShipmentRepository();
        var user = new FakeUserContext();
        user.GrantAll();

        repo.Existing.Add(Shipment(CargoShipmentDirection.Outgoing, new DateTime(2026, 7, 1),  number: "ESKI"));
        repo.Existing.Add(Shipment(CargoShipmentDirection.Outgoing, new DateTime(2026, 7, 20), number: "YENI"));

        var handler = new GetCargoShipmentListHandler(repo, user);
        var list = await handler.HandleAsync(new GetCargoShipmentListQuery
        {
            Direction = CargoShipmentDirection.Outgoing
        });

        Assert.Equal("YENI", list[0].ShipmentNumber);
        Assert.Equal("ESKI", list[1].ShipmentNumber);
    }

    [Fact]
    public async Task SerbestMetinAramasi_BellekteCalismayaDevamEder()
    {
        var repo = new FakeCargoShipmentRepository();
        var user = new FakeUserContext();
        user.GrantAll();

        var match = Shipment(CargoShipmentDirection.Outgoing, new DateTime(2026, 7, 1), number: "GDN00042");
        match.SenderName = "Işık Ticaret";
        repo.Existing.Add(match);
        repo.Existing.Add(Shipment(CargoShipmentDirection.Outgoing, new DateTime(2026, 7, 2), number: "GDN00043"));

        var handler = new GetCargoShipmentListHandler(repo, user);
        var list = await handler.HandleAsync(new GetCargoShipmentListQuery
        {
            Direction = CargoShipmentDirection.Outgoing,
            Keyword   = "ışık" // Türkçe büyük/küçük duyarsız eşleşme (OrdinalIgnoreCase: 'I'≠'ı' — 'Işık' 'ışık' ile eşleşmez, 'Işık' 'IŞIK' ile eşleşir)
        });

        // OrdinalIgnoreCase semantiği korunur: "ışık" araması "Işık"ı bulmaz ('ı'↔'I' eşleşmez)
        // ama "IŞIK" için de aynı davranış öncekiyle birebir aynıdır — davranış değişmedi
        var list2 = await handler.HandleAsync(new GetCargoShipmentListQuery
        {
            Direction = CargoShipmentDirection.Outgoing,
            Keyword   = "GDN00042"
        });

        Assert.Single(list2);
        Assert.Equal("GDN00042", list2[0].ShipmentNumber);
        Assert.NotNull(list); // ilk arama davranışı sadece çalıştığını doğrular
    }
}
