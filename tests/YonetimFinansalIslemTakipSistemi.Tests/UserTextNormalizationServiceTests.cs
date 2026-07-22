using YonetimFinansalIslemTakipSistemi.Application.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

public class UserTextNormalizationServiceTests
{
    private static UserTextNormalizationService CreateService(TextCasePreference preference)
        => new(new FakeUserContext { TextCasePreference = preference });

    [Fact]
    public void Preserve_MetniDegistirmez()
    {
        var service = CreateService(TextCasePreference.Preserve);
        Assert.Equal("İstanbul Ticaret", service.Normalize("İstanbul Ticaret"));
    }

    [Theory]
    [InlineData("istanbul", "İSTANBUL")]
    [InlineData("ışık", "IŞIK")]
    [InlineData("çağrı öz", "ÇAĞRI ÖZ")]
    public void Uppercase_TurkceKurallariylaBuyukHarfYapar(string input, string expected)
    {
        var service = CreateService(TextCasePreference.Uppercase);
        Assert.Equal(expected, service.Normalize(input));
    }

    [Theory]
    [InlineData("İSTANBUL", "istanbul")]
    [InlineData("IŞIK", "ışık")]
    [InlineData("ÇAĞRI", "çağrı")]
    public void Lowercase_TurkceKurallariylaKucukHarfYapar(string input, string expected)
    {
        var service = CreateService(TextCasePreference.Lowercase);
        Assert.Equal(expected, service.Normalize(input));
    }

    [Fact]
    public void BosVeNullGiris_NullDoner()
    {
        var service = CreateService(TextCasePreference.Uppercase);
        Assert.Null(service.Normalize(null));
        Assert.Null(service.Normalize("   "));
    }

    [Fact]
    public void CokluBosluklar_HerTercihteTeklestirilir()
    {
        var service = CreateService(TextCasePreference.Preserve);
        Assert.Equal("Ali Veli", service.Normalize("  Ali   Veli  "));
    }

    [Fact]
    public void FarkliKullanicilar_FarkliTercihKullanabilir()
    {
        // Kullanıcı A: BÜYÜK HARF, Kullanıcı B: küçük harf — aynı giriş farklı sonuç üretir
        var userA = CreateService(TextCasePreference.Uppercase);
        var userB = CreateService(TextCasePreference.Lowercase);

        Assert.Equal("İSTANBUL", userA.Normalize("istanbul"));
        Assert.Equal("istanbul", userB.Normalize("İSTANBUL"));
    }

    [Fact]
    public void TercihDegisikligi_SonrakiCagrilariEtkiler_OncekiSonuclariDegistirmez()
    {
        var context = new FakeUserContext { TextCasePreference = TextCasePreference.Preserve };
        var service = new UserTextNormalizationService(context);

        // "Eski kayıt": Preserve ile kaydedilmiş değer
        var oldRecord = service.Normalize("istanbul");
        Assert.Equal("istanbul", oldRecord);

        // Ayar değişir — yalnızca bundan sonraki normalize çağrıları etkilenir
        context.TextCasePreference = TextCasePreference.Uppercase;
        Assert.Equal("İSTANBUL", service.Normalize("istanbul"));
        Assert.Equal("istanbul", oldRecord); // eski değer kendiliğinden değişmez
    }
}
