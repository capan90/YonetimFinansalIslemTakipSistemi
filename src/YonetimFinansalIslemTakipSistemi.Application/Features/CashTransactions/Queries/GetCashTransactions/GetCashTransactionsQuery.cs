using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCashTransactions;

/// <summary>
/// Listeleme ekranındaki filtre parametreleri. Null olan filtreler görmezden gelinir.
/// </summary>
public class GetCashTransactionsQuery
{
    public DateTime?        DateFrom        { get; set; }
    public DateTime?        DateTo          { get; set; }
    public TransactionType? TransactionType { get; set; }
    public CurrencyType?    CurrencyType    { get; set; }
}
