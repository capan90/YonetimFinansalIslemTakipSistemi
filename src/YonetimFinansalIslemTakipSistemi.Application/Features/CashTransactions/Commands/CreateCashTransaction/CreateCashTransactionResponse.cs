using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.CreateCashTransaction;

/// <summary>
/// Başarılı kayıt sonrası UI'ye dönen özet. Onay dialogunda gösterilir.
/// </summary>
public class CreateCashTransactionResponse
{
    public Guid Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public TransactionType TransactionType { get; set; }
    public CurrencyType CurrencyType { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
