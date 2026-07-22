using YonetimFinansalIslemTakipSistemi.Application.Common;

namespace YonetimFinansalIslemTakipSistemi.Tests;

public class UrlValidatorTests
{
    [Theory]
    [InlineData("https://selfservis.yurticikargo.com/Login.aspx?ReturnUrl=%2fMain.aspx")]
    [InlineData("http://ornek.com")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GecerliVeyaBosUrl_KabulEdilir(string? url)
    {
        Assert.True(UrlValidator.IsValidHttpUrlOrEmpty(url));
    }

    [Theory]
    [InlineData("ftp://ornek.com")]
    [InlineData("dosya-yolu")]
    [InlineData("javascript:alert(1)")]
    [InlineData("www.eksik-sema.com")]
    public void GecersizUrl_Reddedilir(string url)
    {
        Assert.False(UrlValidator.IsValidHttpUrlOrEmpty(url));
    }
}
