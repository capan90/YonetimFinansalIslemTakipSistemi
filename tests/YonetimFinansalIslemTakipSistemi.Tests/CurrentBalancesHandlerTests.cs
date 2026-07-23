using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCurrentBalances;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Bakiye matematiği: Giriş (+) / Çıkış (−) işareti ve para birimi ayrımı.
/// Ana ekran bakiye barının tek veri kaynağı — Sprint 17'de eklendi.
/// </summary>
public class CurrentBalancesHandlerTests
{
    private static CashTransaction Tx(TransactionType type, CurrencyType currency, decimal amount, int dayOffset = 0)
        => new()
        {
            Id              = Guid.NewGuid(),
            TransactionDate = DateTime.UtcNow.Date.AddDays(dayOffset),
            TransactionType = type,
            CurrencyType    = currency,
            Amount          = amount,
            Description     = "test"
        };

    [Fact]
    public async Task BosListe_TumBakiyelerSifir()
    {
        var handler = new GetCurrentBalancesHandler(new FakeCashTransactionRepository());

        var result = await handler.HandleAsync();

        Assert.Equal(0m, result.TlBalance);
        Assert.Equal(0m, result.UsdBalance);
        Assert.Equal(0m, result.EurBalance);
    }

    [Fact]
    public async Task GirisArti_CikisEksi_ParaBirimiAyrimiKorunur()
    {
        var repo = new FakeCashTransactionRepository();
        repo.Items.AddRange(
        [
            Tx(TransactionType.Giris, CurrencyType.TRY, 1000m, -3),
            Tx(TransactionType.Cikis, CurrencyType.TRY,  400m, -2),
            Tx(TransactionType.Giris, CurrencyType.USD,  300m, -2),
            Tx(TransactionType.Cikis, CurrencyType.USD,   50m, -1),
            Tx(TransactionType.Giris, CurrencyType.EUR,  200m, -1),
        ]);

        var result = await new GetCurrentBalancesHandler(repo).HandleAsync();

        Assert.Equal(600m, result.TlBalance);   // 1000 − 400
        Assert.Equal(250m, result.UsdBalance);  //  300 −  50
        Assert.Equal(200m, result.EurBalance);  //  200
    }

    [Fact]
    public async Task NegatifBakiye_MumkunVeDogruHesaplanir()
    {
        var repo = new FakeCashTransactionRepository();
        repo.Items.AddRange(
        [
            Tx(TransactionType.Giris, CurrencyType.TRY, 100m, -1),
            Tx(TransactionType.Cikis, CurrencyType.TRY, 250m),
        ]);

        var result = await new GetCurrentBalancesHandler(repo).HandleAsync();

        Assert.Equal(-150m, result.TlBalance);
    }

    [Fact]
    public async Task SilinmisKayitlar_BakiyeyeDahilEdilmez()
    {
        var repo = new FakeCashTransactionRepository();
        var deleted = Tx(TransactionType.Giris, CurrencyType.TRY, 9999m);
        deleted.IsDeleted = true;
        repo.Items.Add(deleted);
        repo.Items.Add(Tx(TransactionType.Giris, CurrencyType.TRY, 100m));

        var result = await new GetCurrentBalancesHandler(repo).HandleAsync();

        Assert.Equal(100m, result.TlBalance);
    }

    [Fact]
    public async Task KurusHassasiyeti_DecimalIleKorunur()
    {
        var repo = new FakeCashTransactionRepository();
        repo.Items.AddRange(
        [
            Tx(TransactionType.Giris, CurrencyType.TRY, 0.10m),
            Tx(TransactionType.Giris, CurrencyType.TRY, 0.20m),
            Tx(TransactionType.Cikis, CurrencyType.TRY, 0.05m),
        ]);

        var result = await new GetCurrentBalancesHandler(repo).HandleAsync();

        Assert.Equal(0.25m, result.TlBalance);
    }
}
