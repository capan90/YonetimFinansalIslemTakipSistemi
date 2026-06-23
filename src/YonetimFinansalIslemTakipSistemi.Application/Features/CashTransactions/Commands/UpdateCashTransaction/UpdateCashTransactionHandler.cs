using System.Globalization;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.UpdateCashTransaction;

/// <summary>
/// Mevcut nakit işlemi günceller.
/// Soft delete global query filter devrede — bulunamayan kayıt silinmiş veya mevcut değil demektir.
/// </summary>
public class UpdateCashTransactionHandler
{
    private readonly ICashTransactionRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;

    public UpdateCashTransactionHandler(
        ICashTransactionRepository repository,
        IAuditLogService auditLogService,
        IUserContext userContext)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
    }

    public async Task<OperationResult<bool>> HandleAsync(UpdateCashTransactionRequest request)
    {
        if (!_userContext.HasPermission(PermissionType.CanEditTransaction))
            return OperationResult<bool>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        var validationError = Validate(request);
        if (validationError is not null)
            return OperationResult<bool>.Fail(validationError);

        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity is null)
            return OperationResult<bool>.Fail("İşlem bulunamadı.");

        // Mutation öncesi alan değerlerini sakla — diff için
        var prevDate     = entity.TransactionDate;
        var prevType     = entity.TransactionType;
        var prevCurrency = entity.CurrencyType;
        var prevAmount   = entity.Amount;
        var prevDesc     = entity.Description;

        // DatePicker Local/Unspecified döndürebilir; Npgsql timestamptz için UTC zorunlu
        entity.TransactionDate = DateTime.SpecifyKind(request.TransactionDate.Date, DateTimeKind.Utc);
        entity.TransactionType = request.TransactionType;
        entity.CurrencyType    = request.CurrencyType;
        entity.Amount          = request.Amount;
        entity.Description     = request.Description;
        entity.UpdatedByUserId = request.UpdatedByUserId;
        entity.UpdatedAt       = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);

        // Yalnızca değişen alanları audit'e yaz
        var oldParts = new List<string>();
        var newParts = new List<string>();

        if (prevDate.Date != entity.TransactionDate.Date)
        {
            oldParts.Add($"Tarih: {prevDate:dd.MM.yyyy}");
            newParts.Add($"Tarih: {entity.TransactionDate:dd.MM.yyyy}");
        }
        if (prevType != entity.TransactionType)
        {
            oldParts.Add($"Tip: {prevType}");
            newParts.Add($"Tip: {entity.TransactionType}");
        }
        if (prevCurrency != entity.CurrencyType)
        {
            oldParts.Add($"Para Birimi: {prevCurrency}");
            newParts.Add($"Para Birimi: {entity.CurrencyType}");
        }
        if (prevAmount != entity.Amount)
        {
            oldParts.Add($"Tutar: {prevAmount.ToString("N2", new CultureInfo("tr-TR"))}");
            newParts.Add($"Tutar: {entity.Amount.ToString("N2", new CultureInfo("tr-TR"))}");
        }
        if (prevDesc != entity.Description)
        {
            oldParts.Add($"Açıklama: {prevDesc}");
            newParts.Add($"Açıklama: {entity.Description}");
        }

        await _auditLogService.WriteAsync(
            AuditAction.TransactionUpdated,
            _userContext.UserId,
            _userContext.FullName,
            "CashTransaction", entity.Id,
            oldParts.Count > 0 ? string.Join(" | ", oldParts) : null,
            newParts.Count > 0 ? string.Join(" | ", newParts) : null);

        return OperationResult<bool>.Ok(true);
    }

    private static string? Validate(UpdateCashTransactionRequest request)
    {
        // Oturum açık kullanıcı zorunlu — audit kaydı için
        if (request.UpdatedByUserId == Guid.Empty)
            return "İşlemi yapan kullanıcı belirtilmelidir.";

        if (string.IsNullOrWhiteSpace(request.Description))
            return "Açıklama alanı zorunludur. Lütfen işlem açıklaması giriniz.";

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
