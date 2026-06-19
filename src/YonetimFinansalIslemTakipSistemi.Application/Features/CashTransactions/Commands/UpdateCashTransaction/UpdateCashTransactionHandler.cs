using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.UpdateCashTransaction;

/// <summary>
/// Mevcut nakit işlemi günceller.
/// Soft delete global query filter devrede — bulunamayan kayıt silinmiş veya mevcut değil demektir.
/// </summary>
public class UpdateCashTransactionHandler
{
    private readonly ICashTransactionRepository _repository;

    public UpdateCashTransactionHandler(ICashTransactionRepository repository)
        => _repository = repository;

    public async Task<OperationResult<bool>> HandleAsync(UpdateCashTransactionRequest request)
    {
        var validationError = Validate(request);
        if (validationError is not null)
            return OperationResult<bool>.Fail(validationError);

        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity is null)
            return OperationResult<bool>.Fail("İşlem bulunamadı.");

        // DatePicker Local/Unspecified döndürebilir; Npgsql timestamptz için UTC zorunlu
        entity.TransactionDate = DateTime.SpecifyKind(request.TransactionDate.Date, DateTimeKind.Utc);
        entity.TransactionType = request.TransactionType;
        entity.CurrencyType    = request.CurrencyType;
        entity.Amount          = request.Amount;
        entity.Description     = request.Description;
        // Audit: kimin, ne zaman güncellediği
        entity.UpdatedByUserId = request.UpdatedByUserId;
        entity.UpdatedAt       = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);
        return OperationResult<bool>.Ok(true);
    }

    private static string? Validate(UpdateCashTransactionRequest request)
    {
        // Oturum açık kullanıcı zorunlu — audit kaydı için
        if (request.UpdatedByUserId == Guid.Empty)
            return "İşlemi yapan kullanıcı belirtilmelidir.";

        if (request.Amount <= 0)
            return "Tutar sıfırdan büyük olmalıdır.";

        if (request.TransactionDate == default)
            return "İşlem tarihi geçersiz.";

        if (request.TransactionDate.Date > DateTime.Today)
            return "İşlem tarihi bugünden ileri olamaz.";

        if (!Enum.IsDefined(typeof(TransactionType), request.TransactionType))
            return "Geçersiz işlem tipi.";

        if (!Enum.IsDefined(typeof(CurrencyType), request.CurrencyType))
            return "Geçersiz para birimi.";

        return null;
    }
}
