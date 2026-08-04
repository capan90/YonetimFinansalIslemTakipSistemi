using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoPartySuggestions;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using CargoShipmentEntity = YonetimFinansalIslemTakipSistemi.Domain.Entities.CargoShipment;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Gönderi/teslim isim önerileri geçmiş kargo kayıtlarından türetilir — ayrı rehber
/// tablosu yoktur. Liste son kullanılan önce sıralı, tekilleştirilmiş ve yön bazlıdır.
/// </summary>
public class CargoPartySuggestionsTests
{
    private static CargoShipmentEntity Record(
        CargoShipmentDirection direction,
        DateTime createdAt,
        string? sender      = null,
        string? receiver    = null,
        string? deliveredBy = null,
        string? receivedBy  = null) => new()
        {
            Id           = Guid.NewGuid(),
            Direction    = direction,
            CreatedAt    = createdAt,
            ShipmentDate = createdAt.Date,
            SenderName   = sender,
            ReceiverName = receiver,
            DeliveredBy  = deliveredBy,
            ReceivedBy   = receivedBy
        };

    private static (GetCargoPartySuggestionsHandler Handler, FakeCargoShipmentRepository Repo) Build()
    {
        var repo = new FakeCargoShipmentRepository();
        return (new GetCargoPartySuggestionsHandler(repo), repo);
    }

    [Fact]
    public async Task Oneriler_SonKullanilanOnce_Siralanir()
    {
        var (handler, repo) = Build();
        var basel = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

        repo.Existing.Add(Record(CargoShipmentDirection.Outgoing, basel,                sender: "Eski Kişi"));
        repo.Existing.Add(Record(CargoShipmentDirection.Outgoing, basel.AddDays(2), sender: "Yeni Kişi"));
        repo.Existing.Add(Record(CargoShipmentDirection.Outgoing, basel.AddDays(1), sender: "Orta Kişi"));

        var result = await handler.HandleAsync(
            new GetCargoPartySuggestionsQuery(CargoShipmentDirection.Outgoing));

        Assert.Equal(["Yeni Kişi", "Orta Kişi", "Eski Kişi"], result.SenderNames);
    }

    [Fact]
    public async Task AyniIsim_BuyukKucukVeBoslukFarkiGozetmedenTekillestirilir()
    {
        var (handler, repo) = Build();
        var basel = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

        repo.Existing.Add(Record(CargoShipmentDirection.Outgoing, basel.AddDays(2), deliveredBy: "  Ahmet Yılmaz  "));
        repo.Existing.Add(Record(CargoShipmentDirection.Outgoing, basel.AddDays(1), deliveredBy: "ahmet yılmaz"));
        repo.Existing.Add(Record(CargoShipmentDirection.Outgoing, basel,                deliveredBy: "AHMET YILMAZ"));

        var result = await handler.HandleAsync(
            new GetCargoPartySuggestionsQuery(CargoShipmentDirection.Outgoing));

        // En güncel kullanımın yazımı korunur (kırpılmış hâliyle)
        Assert.Equal(["Ahmet Yılmaz"], result.DeliveredBy);
    }

    [Fact]
    public async Task Oneriler_YonBazlidir_DigerYonunIsimleriSizmaz()
    {
        var (handler, repo) = Build();
        var basel = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

        repo.Existing.Add(Record(CargoShipmentDirection.Incoming, basel, sender: "Gelen Gönderici"));
        repo.Existing.Add(Record(CargoShipmentDirection.Outgoing, basel, sender: "Giden Gönderici"));

        var incoming = await handler.HandleAsync(
            new GetCargoPartySuggestionsQuery(CargoShipmentDirection.Incoming));

        Assert.Equal(["Gelen Gönderici"], incoming.SenderNames);
        Assert.DoesNotContain("Giden Gönderici", incoming.SenderNames);
    }

    [Fact]
    public async Task BosVeNullDegerler_OnerilereGirmez()
    {
        var (handler, repo) = Build();
        var basel = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

        repo.Existing.Add(Record(CargoShipmentDirection.Incoming, basel, receivedBy: "Gerçek Kişi", sender: "   "));
        repo.Existing.Add(Record(CargoShipmentDirection.Incoming, basel.AddHours(1), receivedBy: null, sender: null));

        var result = await handler.HandleAsync(
            new GetCargoPartySuggestionsQuery(CargoShipmentDirection.Incoming));

        Assert.Equal(["Gerçek Kişi"], result.ReceivedBy);
        Assert.Empty(result.SenderNames);
    }

    [Fact]
    public async Task KayitYoksa_BosListelerDoner_FormCalismayaDevamEder()
    {
        var (handler, _) = Build();

        var result = await handler.HandleAsync(
            new GetCargoPartySuggestionsQuery(CargoShipmentDirection.Outgoing));

        Assert.Empty(result.SenderNames);
        Assert.Empty(result.ReceiverNames);
        Assert.Empty(result.DeliveredBy);
        Assert.Empty(result.ReceivedBy);
    }
}
