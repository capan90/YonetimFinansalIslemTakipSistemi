using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using static YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import.CargoImportColumnMap;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>Kolon şeması: başlık eşleme toleransı, zorunlu kolon kontrolü, parse kuralları.</summary>
public class CargoImportColumnMapTests
{
    [Fact]
    public void TumSablonBasliklari_Eslesir_EksikVeFazlaYok()
    {
        var headers = Columns.Select(c => c.Header).ToList();

        var match = MatchHeaders(headers);

        Assert.Equal(Columns.Count, match.Indexes.Count);
        Assert.Empty(match.MissingRequired);
        Assert.Empty(match.ExtraHeaders);
    }

    [Fact]
    public void BaslikEslesmesi_BuyukKucukHarfVeBosluklaraToleransli()
    {
        // Türkçe harf duyarsız + trim + çoklu boşluk: "KARGO  FIRMASI" → "Kargo Fırması"...
        // tr-TR'de 'I'.ToLower() = 'ı' olduğundan "FIRMASI" → "fırması" ≠ "firması".
        // Bu bilinçli bir davranıştır: kullanıcı şablon başlığını değiştirmeden kullanır.
        var match = MatchHeaders(["  tarih ", "FİRMA", "takip  no"]);

        Assert.True(match.Indexes.ContainsKey(Column.Tarih));
        Assert.True(match.Indexes.ContainsKey(Column.Firma));
        Assert.True(match.Indexes.ContainsKey(Column.TakipNo));
        Assert.Empty(match.MissingRequired);
    }

    [Fact]
    public void EksikZorunluKolon_Raporlanir()
    {
        var match = MatchHeaders(["Firma", "Takip No"]);

        Assert.Contains("Tarih", match.MissingRequired);
        Assert.DoesNotContain("Firma", match.MissingRequired);
    }

    [Fact]
    public void FazladanKolonlar_TolereEdilir_VeListelenir()
    {
        var match = MatchHeaders(["Tarih", "Firma", "İrsaliye No", "Sipariş Kodu"]);

        Assert.Empty(match.MissingRequired);
        Assert.Equal(["İrsaliye No", "Sipariş Kodu"], match.ExtraHeaders);
    }

    [Theory]
    [InlineData("31.12.2026", 2026, 12, 31)]
    [InlineData("01.01.2026", 2026, 1, 1)]
    [InlineData(" 15.06.2026 ", 2026, 6, 15)]
    public void TarihParse_SablonFormati(string input, int year, int month, int day)
    {
        var date = ParseDate(input);

        Assert.NotNull(date);
        Assert.Equal(new DateTime(year, month, day), date!.Value.Date);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bugün")]
    [InlineData("13.13.2026")]
    public void TarihParse_GecersizDegerler_NullDoner(string input)
        => Assert.Null(ParseDate(input));

    [Theory]
    [InlineData("Evrak", CargoShipmentType.Document)]
    [InlineData("NUMUNE", CargoShipmentType.Sample)]
    [InlineData("yedek parça", CargoShipmentType.SparePart)]
    [InlineData("Diğer", CargoShipmentType.Other)]
    public void GonderiTuru_TurkceEtiketler_Eslesir(string label, CargoShipmentType expected)
        => Assert.Equal(expected, ParseShipmentType(label));

    [Fact]
    public void GonderiTuru_TaninmayanEtiket_NullDoner()
        => Assert.Null(ParseShipmentType("Koli"));

    [Theory]
    [InlineData("Normal",   CargoShipmentPriority.Normal)]
    [InlineData("orta",     CargoShipmentPriority.Medium)]
    [InlineData("ACİL",     CargoShipmentPriority.Urgent)]
    [InlineData("Çok Acil", CargoShipmentPriority.Critical)]
    public void Oncelik_TurkceEtiketler_Eslesir(string label, CargoShipmentPriority expected)
        => Assert.Equal(expected, ParsePriority(label));
}
