using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCashTransactions;

/// <summary>
/// Filtreleme parametrelerine göre nakit işlem listesini döndürür.
/// </summary>
public class GetCashTransactionsHandler
{
    private readonly ICashTransactionRepository _repository;

    public GetCashTransactionsHandler(ICashTransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<CashTransactionDto>> HandleAsync(GetCashTransactionsQuery query)
    {
        var entities = await _repository.GetFilteredAsync(
            query.DateFrom,
            query.DateTo,
            query.TransactionType,
            query.CurrencyType);

        return entities.Select(Map).ToList();
    }

    private static CashTransactionDto Map(CashTransaction e) => new()
    {
        Id                     = e.Id,
        TransactionDate        = e.TransactionDate,
        TransactionTypeDisplay = e.TransactionType switch
        {
            TransactionType.Tahsilat    => "Tahsilat",
            TransactionType.Odeme       => "Ödeme",
            TransactionType.Avans       => "Avans",
            TransactionType.OzelHarcama => "Özel Harcama",
            TransactionType.Transfer    => "Transfer",
            _                           => e.TransactionType.ToString()
        },
        CurrencyTypeDisplay = e.CurrencyType switch
        {
            CurrencyType.TRY => "TRY",
            CurrencyType.USD => "USD",
            CurrencyType.EUR => "EUR",
            _                => e.CurrencyType.ToString()
        },
        Amount      = e.Amount,
        Description = e.Description,
        CreatedAt   = e.CreatedAt
    };
}
