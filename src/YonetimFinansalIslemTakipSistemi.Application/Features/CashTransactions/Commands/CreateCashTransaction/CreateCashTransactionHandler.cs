using System.Globalization;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
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
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;

    public CreateCashTransactionHandler(
        ICashTransactionRepository repository,
        IAuditLogService auditLogService,
        IUserContext userContext)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
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
            // DatePicker Local/Unspecified döndürebilir; Npgsql timestamptz için UTC zorunlu
            TransactionDate = DateTime.SpecifyKind(request.TransactionDate.Date, DateTimeKind.Utc),
            TransactionType = request.TransactionType,
            CurrencyType = request.CurrencyType,
            Amount = request.Amount,
            Description = request.Description,
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _repository.AddAsync(entity);

        // Audit: işlem oluşturuldu
        var newValues = FormatTransaction(entity.TransactionDate, entity.TransactionType,
                                          entity.CurrencyType, entity.Amount, entity.Description);
        await _auditLogService.WriteAsync(
            AuditAction.TransactionCreated,
            _userContext.UserId,
            _userContext.FullName,
            "CashTransaction", entity.Id,
            null, newValues);

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

    private static string FormatTransaction(
        DateTime date, TransactionType type, CurrencyType currency, decimal amount, string description)
        => $"Tarih: {date:dd.MM.yyyy} | Tip: {type} | Para Birimi: {currency} | " +
           $"Tutar: {amount.ToString("N2", new CultureInfo("tr-TR"))} | Açıklama: {description}";

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
