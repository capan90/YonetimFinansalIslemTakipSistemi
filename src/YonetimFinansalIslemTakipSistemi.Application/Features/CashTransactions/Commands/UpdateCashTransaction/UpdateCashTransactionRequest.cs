using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.UpdateCashTransaction;

public class UpdateCashTransactionRequest
{
    public Guid Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public TransactionType TransactionType { get; set; }
    public CurrencyType CurrencyType { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Açıklama opsiyonel; boş bırakılabilir.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Oturumu açık kullanıcının kimliği; audit kaydı için zorunlu.</summary>
    public Guid UpdatedByUserId { get; set; }
}
