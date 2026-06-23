using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Domain.Extensions;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCurrentBalances;

/// <summary>
/// Tüm aktif işlemler üzerinden TL/USD/EUR kümülatif bakiyelerini hesaplar.
/// GetCashTransactionsHandler ile aynı running-balance mantığını kullanır;
/// filtre yoktur — her zaman güncel genel bakiyeyi döndürür.
/// </summary>
public class GetCurrentBalancesHandler
{
    private readonly ICashTransactionRepository _repository;

    public GetCurrentBalancesHandler(ICashTransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<BalanceSummaryDto> HandleAsync()
    {
        var all = await _repository.GetAllForBalanceAsync();

        decimal tl = 0m, usd = 0m, eur = 0m;

        foreach (var e in all)
        {
            var sign = e.TransactionType.GetFinancialDirection() == FinancialDirection.Inflow ? 1m : -1m;
            switch (e.CurrencyType)
            {
                case CurrencyType.TRY: tl  += sign * e.Amount; break;
                case CurrencyType.USD: usd += sign * e.Amount; break;
                case CurrencyType.EUR: eur += sign * e.Amount; break;
            }
        }

        return new BalanceSummaryDto(tl, usd, eur);
    }
}
