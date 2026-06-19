using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// İleride TCMB veya başka kaynaklardan otomatik kur çekimi için abstraction noktası.
/// V1'de implement edilmez; manuel giriş doğrudan handler'a gider.
/// </summary>
public interface IExchangeRateSource
{
    Task<IReadOnlyList<ExchangeRateSourceData>> FetchAsync(DateTime date);
}

public record ExchangeRateSourceData(CurrencyType CurrencyType, decimal ForexBuying, decimal ForexSelling);
