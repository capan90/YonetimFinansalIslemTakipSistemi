using YonetimFinansalIslemTakipSistemi.Application.Common;

namespace YonetimFinansalIslemTakipSistemi.Tests;

public class PhoneNumberNormalizerTests
{
    [Theory]
    [InlineData("0532 123 45 67")]
    [InlineData("5321234567")]
    [InlineData("+90 532 123 45 67")]
    [InlineData("0090 532 123 45 67")]
    [InlineData("(0532) 123-45-67")]
    public void FarkliYazimlar_AyniNormalizeDegereDusulur(string input)
    {
        Assert.Equal("+905321234567", PhoneNumberNormalizer.NormalizeTr(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("123")]                 // çok kısa
    [InlineData("0212 123 45 67")]      // sabit hat — WhatsApp mobil değil
    [InlineData("053212345")]           // eksik hane
    [InlineData("05321234567890")]      // fazla hane
    public void GecersizNumaralar_NullDoner(string? input)
    {
        Assert.Null(PhoneNumberNormalizer.NormalizeTr(input));
    }
}
