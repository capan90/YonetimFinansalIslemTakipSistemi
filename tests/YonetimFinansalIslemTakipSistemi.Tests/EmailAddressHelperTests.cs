using YonetimFinansalIslemTakipSistemi.Application.Common;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Alıcı/CC alanları çoklu adres kabul eder. Ayrıştırma, normalize ve tekilleştirme
/// tek noktadan yapılır — SMTP gönderici ve mail rehberi aynı kuralı kullanır.
/// </summary>
public class EmailAddressHelperTests
{
    [Theory]
    [InlineData("ornek@firma.com")]
    [InlineData("Ad.Soyad@alt.firma.com.tr")]
    [InlineData("  bosluklu@firma.com  ")]
    public void GecerliAdres_KabulEdilir(string email)
        => Assert.True(EmailAddressHelper.IsValid(email));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("at-isareti-yok.com")]
    [InlineData("nokta@yok")]
    [InlineData("iki@at@isareti.com")]
    [InlineData("bosluk var@firma.com")]
    public void GecersizAdres_Reddedilir(string? email)
        => Assert.False(EmailAddressHelper.IsValid(email));

    [Fact]
    public void Normalize_KucukHarfeCevirirVeKirpar()
        => Assert.Equal("ornek@firma.com", EmailAddressHelper.Normalize("  Ornek@Firma.COM  "));

    [Fact]
    public void Normalize_BosDeger_NullDoner()
        => Assert.Null(EmailAddressHelper.Normalize("   "));

    // ── Çoklu alıcı ayrıştırma ──────────────────────────────────────────────

    [Theory]
    [InlineData("a@x.com;b@y.com")]
    [InlineData("a@x.com, b@y.com")]
    [InlineData("a@x.com  b@y.com")]
    [InlineData(" a@x.com ; ; b@y.com ")]
    public void CokluAdres_FarkliAyraclarlaAyristirilir(string raw)
    {
        var (valid, invalid) = EmailAddressHelper.Parse(raw);

        Assert.Equal(["a@x.com", "b@y.com"], valid);
        Assert.Empty(invalid);
    }

    [Fact]
    public void Parse_MukerrerAdresleri_BuyukKucukFarkiGozetmedenEler()
    {
        var (valid, _) = EmailAddressHelper.Parse("Ali@Firma.com; ali@firma.com; ALI@FIRMA.COM");

        Assert.Equal(["ali@firma.com"], valid);
    }

    [Fact]
    public void Parse_GecersizAdresleri_AyriListedeDondurur()
    {
        var (valid, invalid) = EmailAddressHelper.Parse("dogru@firma.com; bozuk-adres; ikinci@firma.com");

        Assert.Equal(["dogru@firma.com", "ikinci@firma.com"], valid);
        Assert.Equal(["bozuk-adres"], invalid);
    }

    [Fact]
    public void Parse_GirisSirasiniKorur_SonKullanilanUsteCikmaz()
    {
        var (valid, _) = EmailAddressHelper.Parse("z@firma.com; a@firma.com; m@firma.com");

        Assert.Equal(["z@firma.com", "a@firma.com", "m@firma.com"], valid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_BosGiris_IkiBosListeDoner(string? raw)
    {
        var (valid, invalid) = EmailAddressHelper.Parse(raw);

        Assert.Empty(valid);
        Assert.Empty(invalid);
    }

    [Fact]
    public void Join_NoktaliVirgulleBirlestirir()
        => Assert.Equal("a@x.com; b@y.com", EmailAddressHelper.Join(["a@x.com", "b@y.com"]));
}
