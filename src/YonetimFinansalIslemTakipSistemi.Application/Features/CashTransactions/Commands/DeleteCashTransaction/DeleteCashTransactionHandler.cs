using System.Globalization;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.DeleteCashTransaction;

/// <summary>
/// Nakit işlemi soft-delete ile siler. Fiziksel silme yapılmaz.
/// DeletedByUserId audit kaydı için UpdateAsync üzerinden persist edilir;
/// CashTransactionRepository.DeleteAsync bu alanı set etmediğinden UpdateAsync tercih edildi.
/// </summary>
public class DeleteCashTransactionHandler
{
    private readonly ICashTransactionRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;

    public DeleteCashTransactionHandler(
        ICashTransactionRepository repository,
        IAuditLogService auditLogService,
        IUserContext userContext)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
    }

    public async Task<OperationResult<bool>> HandleAsync(DeleteCashTransactionRequest request)
    {
        // Oturum açık kullanıcı zorunlu — audit kaydı için
        if (request.DeletedByUserId == Guid.Empty)
            return OperationResult<bool>.Fail("İşlemi yapan kullanıcı belirtilmelidir.");

        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity is null)
            return OperationResult<bool>.Fail("İşlem bulunamadı.");

        // Silmeden önce eski değerleri yakala — audit için
        var oldValues = FormatTransaction(entity.TransactionDate, entity.TransactionType,
                                          entity.CurrencyType, entity.Amount, entity.Description);

        // Soft delete — fiziksel silme kesinlikle yapılmaz

        entity.IsDeleted       = true;
        entity.DeletedAt       = DateTime.UtcNow;
        entity.DeletedByUserId = request.DeletedByUserId;

        await _repository.UpdateAsync(entity);

        // Audit: işlem silindi
        await _auditLogService.WriteAsync(
            AuditAction.TransactionDeleted,
            _userContext.UserId,
            _userContext.FullName,
            "CashTransaction", entity.Id,
            oldValues, null);

        return OperationResult<bool>.Ok(true);
    }

    private static string FormatTransaction(
        DateTime date, TransactionType type, CurrencyType currency, decimal amount, string description)
        => $"Tarih: {date:dd.MM.yyyy} | Tip: {type} | Para Birimi: {currency} | " +
           $"Tutar: {amount.ToString("N2", new CultureInfo("tr-TR"))} | Açıklama: {description}";
}
