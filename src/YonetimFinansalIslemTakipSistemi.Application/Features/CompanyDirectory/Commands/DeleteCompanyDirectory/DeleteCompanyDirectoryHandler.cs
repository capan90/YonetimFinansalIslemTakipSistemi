using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Commands.DeleteCompanyDirectory;

public class DeleteCompanyDirectoryHandler
{
    private readonly ICompanyDirectoryRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;

    public DeleteCompanyDirectoryHandler(
        ICompanyDirectoryRepository repository,
        IAuditLogService auditLogService,
        IUserContext userContext)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
    }

    public async Task<OperationResult<bool>> HandleAsync(DeleteCompanyDirectoryRequest request)
    {
        if (!_userContext.HasPermission(PermissionType.CanManageCompanyDirectory))
            return OperationResult<bool>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        var entity = await _repository.GetByIdWithTrackingAsync(request.Id);
        if (entity is null)
            return OperationResult<bool>.Fail("Firma rehberi kaydı bulunamadı.");

        var oldValues = $"Firma: {entity.CompanyName} | Adres: {entity.AddressLine}";

        // Soft delete — fiziksel silme yapılmaz
        entity.IsDeleted       = true;
        entity.DeletedAt       = DateTime.UtcNow;
        entity.DeletedByUserId = request.DeletedByUserId;

        await _repository.UpdateAsync(entity);

        await _auditLogService.WriteAsync(
            AuditAction.CompanyDirectoryDeleted,
            _userContext.UserId,
            _userContext.FullName,
            "CompanyDirectory", entity.Id,
            oldValues, null);

        return OperationResult<bool>.Ok(true);
    }
}
