using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.DeleteCargoShipment;

public class DeleteCargoShipmentHandler
{
    private readonly ICargoShipmentRepository    _repository;
    private readonly IAuditLogService            _auditLogService;
    private readonly IUserContext                _userContext;
    private readonly ICargoDashboardCacheService _cache;

    public DeleteCargoShipmentHandler(
        ICargoShipmentRepository    repository,
        IAuditLogService            auditLogService,
        IUserContext                userContext,
        ICargoDashboardCacheService cache)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
        _cache           = cache;
    }

    public async Task<OperationResult<bool>> HandleAsync(DeleteCargoShipmentRequest request)
    {
        var requiredPermission = request.Direction == CargoShipmentDirection.Incoming
            ? PermissionType.CanManageIncomingCargo
            : PermissionType.CanManageOutgoingCargo;

        if (!_userContext.HasPermission(requiredPermission))
            return OperationResult<bool>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity is null)
            return OperationResult<bool>.Fail("Kargo kaydı bulunamadı.");

        var dir = entity.Direction == CargoShipmentDirection.Incoming ? "Gelen" : "Giden";
        var oldValues = $"Yön: {dir} | Tarih: {entity.ShipmentDate:dd.MM.yyyy}";

        entity.IsDeleted       = true;
        entity.DeletedAt       = DateTime.UtcNow;
        entity.DeletedByUserId = request.DeletedByUserId;

        await _repository.UpdateAsync(entity);

        await _auditLogService.WriteAsync(
            AuditAction.CargoShipmentDeleted,
            _userContext.UserId,
            _userContext.FullName,
            "CargoShipment", entity.Id,
            oldValues, null);

        // Silme sonrası dashboard cache geçersiz
        _cache.Invalidate();
        return OperationResult<bool>.Ok(true);
    }
}
