namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.DeleteCashTransaction;

public class DeleteCashTransactionRequest
{
    public Guid Id { get; set; }

    /// <summary>Oturumu açık kullanıcının kimliği; audit kaydı için zorunlu.</summary>
    public Guid DeletedByUserId { get; set; }
}
