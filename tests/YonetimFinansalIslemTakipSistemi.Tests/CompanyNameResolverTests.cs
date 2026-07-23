using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using static YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import.CompanyNameResolver;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>Firma adı çözümleme: normalize eşleşme, muğlaklık, pasiflik ve öneri.</summary>
public class CompanyNameResolverTests
{
    private static readonly Guid AcmeId  = Guid.NewGuid();
    private static readonly Guid InciId  = Guid.NewGuid();

    private static CompanyNameResolver Build() => new(
    [
        new Entry(AcmeId,        "Acme Lojistik A.Ş.", IsActive: true),
        new Entry(InciId,        "İNCİ Ticaret",       IsActive: true),
        new Entry(Guid.NewGuid(), "Pasif Firma",       IsActive: false),
    ]);

    [Theory]
    [InlineData("Acme Lojistik A.Ş.")]
    [InlineData("acme  lojistik a.ş.")]
    [InlineData("  ACME LOJİSTİK A.Ş.  ")]
    public void NormalizeEslesme_TekSonuc(string query)
    {
        var result = Build().Resolve(query);

        Assert.Equal(MatchKind.Single, result.Kind);
        Assert.Equal(AcmeId, result.Match!.Id);
    }

    [Fact]
    public void TurkceHarfDuyarsiz_InciBulunur()
    {
        // tr-TR: "inci ticaret" ↔ "İNCİ Ticaret" aynı normalize forma iner
        var result = Build().Resolve("inci ticaret");

        Assert.Equal(MatchKind.Single, result.Kind);
        Assert.Equal(InciId, result.Match!.Id);
    }

    [Fact]
    public void BulunamayanAd_OneriDoner()
    {
        var result = Build().Resolve("Acme Lojistik");

        Assert.Equal(MatchKind.NotFound, result.Kind);
        Assert.Equal("Acme Lojistik A.Ş.", result.Suggestion);
    }

    [Fact]
    public void AyniAdaSahipIkiAktifFirma_MuglakSayilir()
    {
        var resolver = new CompanyNameResolver(
        [
            new Entry(Guid.NewGuid(), "Tekrar A.Ş.", IsActive: true),
            new Entry(Guid.NewGuid(), "TEKRAR a.ş.", IsActive: true),
        ]);

        Assert.Equal(MatchKind.Ambiguous, resolver.Resolve("Tekrar A.Ş.").Kind);
    }

    [Fact]
    public void YalnizcaPasifEslesme_InactiveOnlyDoner()
        => Assert.Equal(MatchKind.InactiveOnly, Build().Resolve("Pasif Firma").Kind);

    [Fact]
    public void BosAd_NotFound()
        => Assert.Equal(MatchKind.NotFound, Build().Resolve("   ").Kind);
}
