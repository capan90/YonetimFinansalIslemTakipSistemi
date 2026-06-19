using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.DeleteCashTransaction;

/// <summary>
/// Nakit işlemi soft-delete ile siler. Fiziksel silme yapılmaz.
/// DeletedByUserId audit kaydı için UpdateAsync üzerinden persist edilir;
/// CashTransactionRepository.DeleteAsync bu alanı set etmediğinden UpdateAsync tercih edildi.
/// </summary>
public class DeleteCashTransactionHandler
{
    private readonly ICashTransactionRepository _repository;

    public DeleteCashTransactionHandler(ICashTransactionRepository repository)
        => _repository = repository;

    public async Task<OperationResult<bool>> HandleAsync(DeleteCashTransactionRequest request)
    {
        // Oturum açık kullanıcı zorunlu — audit kaydı için
        if (request.DeletedByUserId == Guid.Empty)
            return OperationResult<bool>.Fail("İşlemi yapan kullanıcı belirtilmelidir.");

        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity is null)
            return OperationResult<bool>.Fail("İşlem bulunamadı.");

        // Soft delete — fiziksel silme kesinlikle yapılmaz
        entity.IsDeleted       = true;
        entity.DeletedAt       = DateTime.UtcNow;
        entity.DeletedByUserId = request.DeletedByUserId;

        await _repository.UpdateAsync(entity);
        return OperationResult<bool>.Ok(true);
    }
}
