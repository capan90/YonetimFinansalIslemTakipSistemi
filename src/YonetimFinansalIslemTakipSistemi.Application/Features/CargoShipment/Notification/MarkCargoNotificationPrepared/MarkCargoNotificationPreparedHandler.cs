using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification.MarkCargoNotificationPrepared;

/// <summary>
/// Kullanıcı "Hazırlandı Olarak İşaretle" butonuna bastığında bildirim durumunu günceller.
/// WhatsApp → WhatsAppPrepared; Mail → MailPrepared.
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

        var entity = await _repository.GetByIdAsync(request.CargoShipmentId);
        if (entity is null)
            return OperationResult<bool>.Fail("Kargo kaydı bulunamadı.");

        entity.UpdatedByUserId = _userContext.UserId;
        entity.UpdatedAt       = DateTime.UtcNow;

        if (request.NotificationType == NotificationType.WhatsApp)
        {
            entity.NotificationStatus = CargoNotificationStatus.WhatsAppPrepared;
            await _repository.UpdateAsync(entity);

            await _auditLogService.WriteAsync(
                AuditAction.CargoWhatsAppPrepared,
                _userContext.UserId,
                _userContext.FullName,
                "CargoShipment",
                entity.Id,
                newValues: $"WhatsApp mesajı hazırlandı | Kargo No: {entity.ShipmentNumber}");
        }
        else if (request.NotificationType == NotificationType.Mail)
        {
            entity.NotificationStatus = CargoNotificationStatus.MailPrepared;
            await _repository.UpdateAsync(entity);

            await _auditLogService.WriteAsync(
                AuditAction.CargoMailPrepared,
                _userContext.UserId,
                _userContext.FullName,
                "CargoShipment",
                entity.Id,
                newValues: $"Mail gönderildi | Kargo No: {entity.ShipmentNumber}");
        }
        else
        {
            return OperationResult<bool>.Fail("Bu bildirim türü henüz desteklenmiyor.");
        }

        return OperationResult<bool>.Ok(true);
    }
}
