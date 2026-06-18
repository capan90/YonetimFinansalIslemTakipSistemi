using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.CreateCashTransaction;

/// <summary>
/// Yeni nakit işlem oluşturma use case'i.
/// Bağlantı yönetimi ve kalıcı depolama Infrastructure'ın sorumluluğu.
/// </summary>
public class CreateCashTransactionHandler
{
    private readonly ICashTransactionRepository _repository;

    public CreateCashTransactionHandler(ICashTransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<CreateCashTransactionResponse>> HandleAsync(
        CreateCashTransactionRequest request)
    {
        var validationError = Validate(request);
        if (validationError is not null)
            return OperationResult<CreateCashTransactionResponse>.Fail(validationError);

        var entity = new CashTransaction
        {
            Id = Guid.NewGuid(),
            TransactionDate = request.TransactionDate,
            TransactionType = request.TransactionType,
            CurrencyType = request.CurrencyType,
            Amount = request.Amount,
            Description = request.Description,
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _repository.AddAsync(entity);

        var response = new CreateCashTransactionResponse
        {
            Id = entity.Id,
            TransactionDate = entity.TransactionDate,
            TransactionType = entity.TransactionType,
            CurrencyType = entity.CurrencyType,
            Amount = entity.Amount,
            CreatedAt = entity.CreatedAt
        };

        return OperationResult<CreateCashTransactionResponse>.Ok(response);
    }

    /// <summary>
    /// Null döner = geçerli. Dolu string = hata mesajı.
    /// </summary>
    private static string? Validate(CreateCashTransactionRequest request)
    {
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

        if (request.CreatedByUserId == Guid.Empty)
            return "İşlemi yapan kullanıcı belirtilmelidir.";

        return null;
    }
}
