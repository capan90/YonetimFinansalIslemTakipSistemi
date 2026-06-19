using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.ExchangeRates.Commands.CreateOrUpdateExchangeRate;

public class CreateOrUpdateExchangeRateCommand
{
    public DateTime     RateDate     { get; set; }
    public CurrencyType CurrencyType { get; set; }
    public decimal      ForexBuying  { get; set; }
    public decimal      ForexSelling { get; set; }
}
