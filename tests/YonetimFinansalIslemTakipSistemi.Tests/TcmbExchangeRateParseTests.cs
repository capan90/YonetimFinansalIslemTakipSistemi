using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// TcmbExchangeRateSource.ParseRates: gerçek TCMB kur XML formatını ağdan bağımsız doğrular.
/// </summary>
public class TcmbExchangeRateParseTests
{
    // TCMB today.xml formatının sadeleştirilmiş ama gerçek yapısı (USD, EUR + alakasız GBP)
    private const string SampleXml = """
        <?xml version="1.0" encoding="ISO-8859-9"?>
        <Tarih_Date Tarih="24.07.2026" Date="07/24/2026" Bulten_No="2026/140">
          <Currency CrossOrder="0" Kod="USD" CurrencyCode="USD">
            <Unit>1</Unit>
            <Isim>ABD DOLARI</Isim>
            <CurrencyName>US DOLLAR</CurrencyName>
            <ForexBuying>47.1647</ForexBuying>
            <ForexSelling>47.2497</ForexSelling>
            <BanknoteBuying>47.1316</BanknoteBuying>
            <BanknoteSelling>47.3206</BanknoteSelling>
          </Currency>
          <Currency CrossOrder="9" Kod="EUR" CurrencyCode="EUR">
            <Unit>1</Unit>
            <Isim>EURO</Isim>
            <CurrencyName>EURO</CurrencyName>
            <ForexBuying>53.6951</ForexBuying>
            <ForexSelling>53.7918</ForexSelling>
            <BanknoteBuying>53.6575</BanknoteBuying>
            <BanknoteSelling>53.8725</BanknoteSelling>
          </Currency>
          <Currency CrossOrder="1" Kod="AUD" CurrencyCode="AUD">
            <Unit>1</Unit>
            <Isim>AVUSTRALYA DOLARI</Isim>
            <ForexBuying>30.1234</ForexBuying>
            <ForexSelling>30.3456</ForexSelling>
          </Currency>
        </Tarih_Date>
        """;

    [Fact]
    public void ParseRates_UsdVeEur_DogruDegerlerleAyristirir()
    {
        var rates = TcmbExchangeRateSource.ParseRates(SampleXml);

        // Yalnızca USD ve EUR alınır (AUD atlanır)
        Assert.Equal(2, rates.Count);

        var usd = Assert.Single(rates, r => r.CurrencyType == CurrencyType.USD);
        Assert.Equal(47.1647m, usd.ForexBuying);
        Assert.Equal(47.2497m, usd.ForexSelling);

        var eur = Assert.Single(rates, r => r.CurrencyType == CurrencyType.EUR);
        Assert.Equal(53.6951m, eur.ForexBuying);
        Assert.Equal(53.7918m, eur.ForexSelling);
    }

    [Fact]
    public void ParseRates_NoktaOndalik_InvariantCultureIleOkunur()
    {
        // tr-TR kültüründe '.' binlik ayırıcıdır; yanlış kültürle 471647 gibi okunmamalı
        var rates = TcmbExchangeRateSource.ParseRates(SampleXml);
        var usd = rates.Single(r => r.CurrencyType == CurrencyType.USD);
        Assert.True(usd.ForexBuying is > 47m and < 48m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<html><body>404 Not Found</body></html>")]
    [InlineData("bozuk xml <<")]
    public void ParseRates_GecersizVeyaBosGirdi_BosListe(string xml)
    {
        var rates = TcmbExchangeRateSource.ParseRates(xml);
        Assert.Empty(rates);
    }

    [Fact]
    public void ParseRates_ForexBosVeyaSifir_ParaBirimiAtlanir()
    {
        // Bazı günlerde bazı kurlar boş yayınlanabilir → o para birimi dahil edilmez
        const string xml = """
            <Tarih_Date>
              <Currency Kod="USD"><ForexBuying></ForexBuying><ForexSelling>47.25</ForexSelling></Currency>
              <Currency Kod="EUR"><ForexBuying>53.69</ForexBuying><ForexSelling>53.79</ForexSelling></Currency>
            </Tarih_Date>
            """;

        var rates = TcmbExchangeRateSource.ParseRates(xml);
        Assert.Single(rates);
        Assert.Equal(CurrencyType.EUR, rates[0].CurrencyType);
    }
}
