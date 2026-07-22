using System.Globalization;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Services;

/// <summary>
/// IUserTextNormalizationService implementasyonu. Tercihi IUserContext'ten okur;
/// böylece Excel import veya farklı bir UI gelse bile aynı kural merkezi olarak korunur.
/// </summary>
public class UserTextNormalizationService : IUserTextNormalizationService
{
    // Türkçe harf dönüşümü zorunlu: istanbul→İSTANBUL, ışık→IŞIK, İSTANBUL→istanbul
    private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");

    private readonly IUserContext _userContext;

    public UserTextNormalizationService(IUserContext userContext)
        => _userContext = userContext;

    public string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        // Boşluk hijyeni her tercihte uygulanır; harf dönüşümü tercihe bağlıdır
        var collapsed = string.Join(" ", value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return _userContext.TextCasePreference switch
        {
            TextCasePreference.Uppercase => collapsed.ToUpper(TrCulture),
            TextCasePreference.Lowercase => collapsed.ToLower(TrCulture),
            _                            => collapsed
        };
    }
}
