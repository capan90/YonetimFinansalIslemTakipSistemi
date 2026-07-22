using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.DeleteWhatsAppContact;

/// <summary>
/// Rehber kişisini soft delete eder. Numara tekrar eklenmek istenirse
/// CreateWhatsAppContactHandler kaydı geri yükler.
/// </summary>
public class DeleteWhatsAppContactHandler
{
    private readonly IWhatsAppContactRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;

    public DeleteWhatsAppContactHandler(
        IWhatsAppContactRepository repository,
        IAuditLogService auditLogService,
        IUserContext userContext)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
    }

    public async Task<OperationResult<bool>> HandleAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null)
            return OperationResult<bool>.Fail("Rehber kaydı bulunamadı.");

        entity.IsDeleted       = true;
        entity.DeletedAt       = DateTime.UtcNow;
        entity.DeletedByUserId = _userContext.UserId;

        await _repository.UpdateAsync(entity);

        // Silme audit'inde ad ve telefon korunur
        await _auditLogService.WriteAsync(
            AuditAction.WhatsAppContactDeleted,
            _userContext.UserId,
            _userContext.FullName,
            "WhatsAppContact", entity.Id,
            $"Ad: {entity.FullName} | Telefon: {entity.Phone} | Firma: {entity.Company ?? "-"}", null);

        return OperationResult<bool>.Ok(true);
    }
}
