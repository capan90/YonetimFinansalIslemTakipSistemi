using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

public interface IExchangeRateRepository
{
    Task<ExchangeRate?> GetByDateAndCurrencyAsync(DateTime rateDateUtc, CurrencyType currency);
    Task<IReadOnlyList<ExchangeRate>> GetFilteredAsync(DateTime? fromUtc, DateTime? toExclusiveUtc, CurrencyType? currency);
    Task AddAsync(ExchangeRate rate);
    Task UpdateAsync(ExchangeRate rate);
}
