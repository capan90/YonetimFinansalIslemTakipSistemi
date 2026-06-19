using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.ExchangeRates.Queries.GetExchangeRates;

public class GetExchangeRatesQuery
{
    public DateTime?     DateFrom     { get; set; }
    public DateTime?     DateTo       { get; set; }
    public CurrencyType? CurrencyType { get; set; }
}
