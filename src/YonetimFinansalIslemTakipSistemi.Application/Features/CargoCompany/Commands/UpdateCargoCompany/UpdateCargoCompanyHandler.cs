using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Commands.UpdateCargoCompany;

public class UpdateCargoCompanyHandler
{
    private readonly ICargoCompanyRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;

    public UpdateCargoCompanyHandler(
        ICargoCompanyRepository repository,
        IAuditLogService auditLogService,
        IUserContext userContext)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
    }

    public async Task<OperationResult<bool>> HandleAsync(UpdateCargoCompanyRequest request)
    {
        if (!_userContext.HasPermission(PermissionType.CanManageCargoCompanies))
            return OperationResult<bool>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return OperationResult<bool>.Fail("Kargo firması adı zorunludur.");

        var entity = await _repository.GetByIdWithTrackingAsync(request.Id);
        if (entity is null)
            return OperationResult<bool>.Fail("Kargo firması bulunamadı.");

        var oldValues = $"Ad: {entity.Name}";

        entity.Name                = request.Name.Trim();
        entity.TrackingUrlTemplate = request.TrackingUrlTemplate?.Trim();
        entity.Phone               = request.Phone?.Trim();
        entity.Website             = request.Website?.Trim();
        entity.Notes               = request.Notes?.Trim();
        entity.IsActive            = request.IsActive;
        entity.UpdatedByUserId     = request.UpdatedByUserId;
        entity.UpdatedAt           = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);

        await _auditLogService.WriteAsync(
            AuditAction.CargoCompanyUpdated,
            _userContext.UserId,
            _userContext.FullName,
            "CargoCompany", entity.Id,
            oldValues, $"Ad: {entity.Name}");

        return OperationResult<bool>.Ok(true);
    }
}
