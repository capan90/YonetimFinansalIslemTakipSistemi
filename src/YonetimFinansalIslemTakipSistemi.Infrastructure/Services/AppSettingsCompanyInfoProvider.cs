using Microsoft.Extensions.Configuration;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// appsettings.json → CompanyInfo["..."] bölümünden firma bilgilerini okur.
/// Anahtar yoksa fallback değerler kullanılır.
/// </summary>
public class AppSettingsCompanyInfoProvider : ICompanyInfoProvider
{
    private readonly IConfiguration _config;

    public AppSettingsCompanyInfoProvider(IConfiguration config)
        => _config = config;

    public CompanyInfo Get()
    {
        var s = _config.GetSection("CompanyInfo");
        return new CompanyInfo(
            Name:     s["Name"]     ?? "Şirket Adı",
            Address:  s["Address"],
            District: s["District"],
            City:     s["City"],
            Phone:    s["Phone"],
            LogoPath: s["LogoPath"]
        );
    }
}
