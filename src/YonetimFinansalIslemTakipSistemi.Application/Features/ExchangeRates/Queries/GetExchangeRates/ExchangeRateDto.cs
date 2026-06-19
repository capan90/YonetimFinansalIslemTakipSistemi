using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.ExchangeRates.Queries.GetExchangeRates;

public class ExchangeRateDto
{
    public Guid         Id              { get; init; }
    public DateTime     RateDate        { get; init; }
    public CurrencyType CurrencyType    { get; init; }
    public string       CurrencyDisplay { get; init; } = string.Empty;
    public decimal      ForexBuying     { get; init; }
    public decimal      ForexSelling    { get; init; }
    public DateTime     CreatedAt       { get; init; }
    public DateTime?    UpdatedAt       { get; init; }
}
