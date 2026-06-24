using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Commands.DeleteCargoCompany;

public class DeleteCargoCompanyHandler
{
    private readonly ICargoCompanyRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;

    public DeleteCargoCompanyHandler(
        ICargoCompanyRepository repository,
        IAuditLogService auditLogService,
        IUserContext userContext)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
    }

    public async Task<OperationResult<bool>> HandleAsync(DeleteCargoCompanyRequest request)
    {
        if (!_userContext.HasPermission(PermissionType.CanManageCargoCompanies))
            return OperationResult<bool>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        var entity = await _repository.GetByIdWithTrackingAsync(request.Id);
        if (entity is null)
            return OperationResult<bool>.Fail("Kargo firması bulunamadı.");

        var oldValues = $"Ad: {entity.Name}";

        entity.IsDeleted       = true;
        entity.DeletedAt       = DateTime.UtcNow;
        entity.DeletedByUserId = request.DeletedByUserId;

        await _repository.UpdateAsync(entity);

        await _auditLogService.WriteAsync(
            AuditAction.CargoCompanyDeleted,
            _userContext.UserId,
            _userContext.FullName,
            "CargoCompany", entity.Id,
            oldValues, null);

        return OperationResult<bool>.Ok(true);
    }
}
