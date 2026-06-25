using YonetimFinansalIslemTakipSistemi.Application.Common;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Gönderici firma bilgilerini sağlar.
/// V1: AppSettings → AppSettingsCompanyInfoProvider.
/// V2: Company Settings modülü eklendiğinde yeni implementasyon yeterlidir.
/// </summary>
public interface ICompanyInfoProvider
{
    CompanyInfo Get();
}
