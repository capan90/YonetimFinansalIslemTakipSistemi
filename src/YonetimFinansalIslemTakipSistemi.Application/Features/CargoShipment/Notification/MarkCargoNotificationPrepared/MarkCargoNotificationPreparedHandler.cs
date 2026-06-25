using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification.MarkCargoNotificationPrepared;

/// <summary>
/// Kullanıcı mesajı kopyaladıktan / WhatsApp Web'i açtıktan sonra
/// bildirim durumunu WhatsAppPrepared'a yükseltir ve audit yazar.
/// Bir sonraki mail sprinti için Mail dalı kolayca eklenebilir.
/// </summary>
public class MarkCargoNotificationPreparedHandler
{
    private readonly ICargoShipmentRepository _repository;
    private readonly IAuditLogService         _auditLogService;
    private readonly IUserContext             _userContext;

    public MarkCargoNotificationPreparedHandler(
        ICargoShipmentRepository repository,
        IAuditLogService auditLogService,
        IUserContext userContext)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
    }

    public async Task<OperationResult<bool>> HandleAsync(
        MarkCargoNotificationPreparedRequest request)
    {
        var requiredPermission = request.Direction == CargoShipmentDirection.Incoming
            ? PermissionType.CanManageIncomingCargo
            : PermissionType.CanManageOutgoingCargo;

        if (!_userContext.HasPermission(requiredPermission))
            return OperationResult<bool>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        if (request.NotificationType != NotificationType.WhatsApp)
            return OperationResult<bool>.Fail("Bu bildirim türü henüz desteklenmiyor.");

        var entity = await _repository.GetByIdAsync(request.CargoShipmentId);
        if (entity is null)
            return OperationResult<bool>.Fail("Kargo kaydı bulunamadı.");

        entity.NotificationStatus = CargoNotificationStatus.WhatsAppPrepared;
        entity.UpdatedByUserId    = _userContext.UserId;
        entity.UpdatedAt          = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);

        // Audit: hangi kargo için, kim tarafından, ne zaman WhatsApp hazırlandı
        await _auditLogService.WriteAsync(
            AuditAction.CargoWhatsAppPrepared,
            _userContext.UserId,
            _userContext.FullName,
            "CargoShipment",
            entity.Id,
            newValues: $"WhatsApp mesajı hazırlandı | Kargo No: {entity.ShipmentNumber}");

        return OperationResult<bool>.Ok(true);
    }
}
