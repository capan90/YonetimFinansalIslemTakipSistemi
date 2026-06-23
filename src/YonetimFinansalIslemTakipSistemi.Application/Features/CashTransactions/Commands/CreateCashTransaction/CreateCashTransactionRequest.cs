using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.CreateCashTransaction;

/// <summary>
/// UI'den Handler'a taşınan ham istek verisi.
/// </summary>
public class CreateCashTransactionRequest
{
    public DateTime TransactionDate { get; set; }
    public TransactionType TransactionType { get; set; }
    public CurrencyType CurrencyType { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Açıklama zorunludur.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Oturumu açık kullanıcının kimliği; audit kaydı için zorunlu.</summary>
    public Guid CreatedByUserId { get; set; }
}
