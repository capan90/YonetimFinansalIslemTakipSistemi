using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Domain.Extensions;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCashTransactions;

/// <summary>
/// Filtreleme parametrelerine göre nakit işlem listesini döndürür.
///
/// Running balance yaklaşımı:
///   Tüm aktif işlemler kronolojik (ASC) sırada çekilir, bakiye hesaplanır.
///   Filtreler hangi satırların görüntüleneceğini belirler; bakiye hesabı filtreye tabi değildir.
///   Böylece tarih filtresi altında bile her satırın bakiyesi gerçek tarihsel değeri yansıtır.
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
        // Tüm aktif kayıtlar kronolojik sırada — bakiye tüm tarih üzerinden hesaplanmalı
        var all = await _repository.GetAllForBalanceAsync();

        // ASC geçiş: her işlem uygulandıktan sonraki per-currency bakiye
        decimal tlBal = 0, usdBal = 0, eurBal = 0;
        var withBalance = new List<TransactionWithBalance>(all.Count);

        foreach (var e in all)
        {
            var sign = e.TransactionType.GetFinancialDirection() == FinancialDirection.Inflow ? 1m : -1m;

            switch (e.CurrencyType)
            {
                case CurrencyType.TRY: tlBal  += sign * e.Amount; break;
                case CurrencyType.USD: usdBal += sign * e.Amount; break;
                case CurrencyType.EUR: eurBal += sign * e.Amount; break;
            }

            withBalance.Add(new TransactionWithBalance(e, tlBal, usdBal, eurBal));
        }

        // Görüntü filtreleri in-memory uygulanır; bakiye zaten gerçek tarihsel değeri taşıyor
        IEnumerable<TransactionWithBalance> filtered = withBalance;

        if (query.DateFrom.HasValue)
            filtered = filtered.Where(x => x.Entity.TransactionDate >= query.DateFrom.Value);
        if (query.DateTo.HasValue)
            filtered = filtered.Where(x => x.Entity.TransactionDate <= query.DateTo.Value);
        if (query.TransactionType.HasValue)
            filtered = filtered.Where(x => x.Entity.TransactionType == query.TransactionType.Value);
        if (query.CurrencyType.HasValue)
            filtered = filtered.Where(x => x.Entity.CurrencyType == query.CurrencyType.Value);
        if (!string.IsNullOrWhiteSpace(query.DescriptionContains))
            filtered = filtered.Where(x =>
                x.Entity.Description != null &&
                x.Entity.Description.Contains(query.DescriptionContains, StringComparison.OrdinalIgnoreCase));

        if (query.AmountOperator is not null && query.AmountValue.HasValue)
        {
            var val = query.AmountValue.Value;
            filtered = query.AmountOperator switch
            {
                ">"  => filtered.Where(x => x.Entity.Amount >  val),
                ">=" => filtered.Where(x => x.Entity.Amount >= val),
                "<"  => filtered.Where(x => x.Entity.Amount <  val),
                "<=" => filtered.Where(x => x.Entity.Amount <= val),
                "="  => filtered.Where(x => x.Entity.Amount == val),
                "!=" => filtered.Where(x => x.Entity.Amount != val),
                _    => filtered
            };
        }

        // Ekranda newest-first; sıralama bakiye doğruluğunu etkilemez
        return filtered
            .OrderByDescending(x => x.Entity.TransactionDate)
            .ThenByDescending(x => x.Entity.CreatedAt)
            .ThenByDescending(x => x.Entity.Id)
            .Select(x => Map(x.Entity, x.TlBalance, x.UsdBalance, x.EurBalance))
            .ToList();
    }

    private static CashTransactionDto Map(CashTransaction e, decimal tl, decimal usd, decimal eur)
    {
        var isInflow = e.TransactionType.GetFinancialDirection() == FinancialDirection.Inflow;
        return new CashTransactionDto
        {
            Id                     = e.Id,
            TransactionDate        = e.TransactionDate,
            TransactionTypeDisplay = e.TransactionType switch
            {
                TransactionType.Giris => "Giriş",
                TransactionType.Cikis => "Çıkış",
                _                     => e.TransactionType.ToString()
            },
            CurrencyTypeDisplay = e.CurrencyType switch
            {
                CurrencyType.TRY => "TRY",
                CurrencyType.USD => "USD",
                CurrencyType.EUR => "EUR",
                _                => e.CurrencyType.ToString()
            },
            // Borç = Giriş tutarı (alan borçludur), Alacak = Çıkış tutarı (veren alacaklıdır); diğeri sıfır
            Borc            = isInflow  ? e.Amount : 0m,
            Alacak          = !isInflow ? e.Amount : 0m,
            Description     = e.Description,
            CreatedAt       = e.CreatedAt,
            TlBalanceAfter  = tl,
            UsdBalanceAfter = usd,
            EurBalanceAfter = eur,
        };
    }

    // Balance hesabı sırasında entity + hesaplanan bakiyeleri birlikte taşır
    private sealed record TransactionWithBalance(
        CashTransaction Entity,
        decimal TlBalance,
        decimal UsdBalance,
        decimal EurBalance);
}
