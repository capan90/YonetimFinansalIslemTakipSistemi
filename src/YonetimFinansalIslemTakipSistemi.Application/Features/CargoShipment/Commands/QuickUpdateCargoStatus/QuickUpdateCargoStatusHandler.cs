using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.QuickUpdateCargoStatus;

public class QuickUpdateCargoStatusHandler
{
    private readonly ICargoShipmentRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;

    public QuickUpdateCargoStatusHandler(
        ICargoShipmentRepository repository,
        IAuditLogService auditLogService,
        IUserContext userContext)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
    }

    public async Task<OperationResult<bool>> HandleAsync(QuickUpdateCargoStatusRequest request)
    {
        var requiredPermission = request.Direction == CargoShipmentDirection.Incoming
            ? PermissionType.CanManageIncomingCargo
            : PermissionType.CanManageOutgoingCargo;

        if (!_userContext.HasPermission(requiredPermission))
            return OperationResult<bool>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity is null)
            return OperationResult<bool>.Fail("Kargo kaydı bulunamadı.");

        if (!CargoStatusTransitions.IsAllowed(entity.Status, request.NewStatus))
            return OperationResult<bool>.Fail(
                $"'{DisplayStatus(entity.Status)}' durumundan '{DisplayStatus(request.NewStatus)}' durumuna geçiş geçersizdir.");

        var oldStatus = entity.Status;
        entity.Status          = request.NewStatus;
        entity.UpdatedByUserId = request.UpdatedByUserId;
        entity.UpdatedAt       = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);

        // Timeline-uyumlu format: sadece değişen alan yazılır
        await _auditLogService.WriteAsync(
            AuditAction.CargoShipmentUpdated,
            _userContext.UserId,
            _userContext.FullName,
            "CargoShipment", entity.Id,
            $"Durum: {DisplayStatus(oldStatus)}",
            $"Durum: {DisplayStatus(request.NewStatus)}");

        return OperationResult<bool>.Ok(true);
    }

    private static string DisplayStatus(CargoShipmentStatus s) => s switch
    {
        CargoShipmentStatus.Draft              => "Taslak",             // eski kayıt etiketi — audit log uyumluluğu
        CargoShipmentStatus.Prepared           => "Gönderime Hazır",
        CargoShipmentStatus.HandedToCargo      => "Kargoya Teslim Edildi",
        CargoShipmentStatus.Shipped            => "Gönderildi",
        CargoShipmentStatus.Waiting            => "Bekleniyor",
        CargoShipmentStatus.Received           => "Teslim Alındı",
        CargoShipmentStatus.PersonnelDelivered => "Personele Teslim Edildi",
        CargoShipmentStatus.Delivered          => "Teslim Edildi",
        CargoShipmentStatus.Cancelled          => "İptal",
        _                                      => s.ToString()
    };
}
