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
    private readonly ISystemLogService           _systemLog;

    public DeleteCargoShipmentHandler(
        ICargoShipmentRepository    repository,
        IAuditLogService            auditLogService,
        IUserContext                userContext,
        ICargoDashboardCacheService cache,
        ISystemLogService           systemLog)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
        _cache           = cache;
        _systemLog       = systemLog;
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
        // Numara audit'te korunur — geri alma numarayı entity'den düşse bile izi burada kalır
        var oldValues = $"Yön: {dir} | No: {entity.ShipmentNumber ?? "-"} | Tarih: {entity.ShipmentDate:dd.MM.yyyy}";

        entity.IsDeleted       = true;
        entity.DeletedAt       = DateTime.UtcNow;
        entity.DeletedByUserId = request.DeletedByUserId;

        // Soft delete + (yalnızca son numaraysa) sayaç geri alma — tek transaction'da.
        // Aradaki silinmiş numaralar asla yeniden kullanılmaz.
        long? reclaimed;
        try
        {
            reclaimed = await _repository.SoftDeleteWithNumberReclaimAsync(entity);
        }
        catch (Exception ex)
        {
            await _systemLog.LogErrorAsync(
                "Cargo", "Kargo kaydı silinemedi.", ex, source: nameof(DeleteCargoShipmentHandler));

            return OperationResult<bool>.Fail(ex is DataStoreException
                ? ex.Message
                : "Kargo kaydı silinemedi. Teknik ayrıntı Sistem Loglarına kaydedildi.");
        }

        await _auditLogService.WriteAsync(
            AuditAction.CargoShipmentDeleted,
            _userContext.UserId,
            _userContext.FullName,
            "CargoShipment", entity.Id,
            oldValues,
            reclaimed is not null ? "Son numara geri alındı — sonraki kayıtta yeniden kullanılacak" : null);

        // Silme sonrası dashboard cache geçersiz
        _cache.Invalidate();
        return OperationResult<bool>.Ok(true);
    }
}
