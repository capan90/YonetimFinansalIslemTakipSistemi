using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts.DeleteMailContact;

/// <summary>
/// Mail rehberi kişisini soft delete eder. Adres tekrar eklenmek istenirse
/// CreateMailContactHandler kaydı geri yükler.
/// </summary>
public class DeleteMailContactHandler
{
    private readonly IMailContactRepository _repository;
    private readonly IAuditLogService       _auditLogService;
    private readonly IUserContext           _userContext;

    public DeleteMailContactHandler(
        IMailContactRepository repository,
        IAuditLogService       auditLogService,
        IUserContext           userContext)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
    }

    public async Task<OperationResult<bool>> HandleAsync(Guid id)
    {
        if (!MailContactPermissions.CanModify(_userContext))
            return OperationResult<bool>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        var entity = await _repository.GetByIdAsync(id);
        if (entity is null)
            return OperationResult<bool>.Fail("Rehber kaydı bulunamadı.");

        entity.IsDeleted       = true;
        entity.DeletedAt       = DateTime.UtcNow;
        entity.DeletedByUserId = _userContext.UserId;

        await _repository.UpdateAsync(entity);

        // Silme audit'inde ad ve adres korunur
        await _auditLogService.WriteAsync(
            AuditAction.MailContactDeleted,
            _userContext.UserId,
            _userContext.FullName,
            "MailContact", entity.Id,
            $"Ad: {entity.FullName} | E-posta: {entity.Email} | Firma: {entity.Company ?? "-"}", null);

        return OperationResult<bool>.Ok(true);
    }
}
