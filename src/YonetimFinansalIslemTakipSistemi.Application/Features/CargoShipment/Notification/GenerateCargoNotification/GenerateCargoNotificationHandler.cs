using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Application.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification.GenerateCargoNotification;

/// <summary>
/// Kargo bildirimi için mesaj modeli üretir.
/// Preview için audit yazılmaz; durum değişmez.
/// Durum güncellemesi ve audit: MarkCargoNotificationPreparedHandler.
/// </summary>
public class GenerateCargoNotificationHandler
{
    private readonly ICargoShipmentRepository    _repository;
    private readonly WhatsAppNotificationComposer _whatsAppComposer;
    private readonly MailNotificationComposer     _mailComposer;
    private readonly IUserContext                 _userContext;

    public GenerateCargoNotificationHandler(
        ICargoShipmentRepository repository,
        WhatsAppNotificationComposer whatsAppComposer,
        MailNotificationComposer mailComposer,
        IUserContext userContext)
    {
        _repository       = repository;
        _whatsAppComposer = whatsAppComposer;
        _mailComposer     = mailComposer;
        _userContext      = userContext;
    }

    public async Task<OperationResult<CargoNotificationModel>> HandleAsync(
        GenerateCargoNotificationRequest request)
    {
        var requiredPermission = request.Direction == CargoShipmentDirection.Incoming
            ? PermissionType.CanManageIncomingCargo
            : PermissionType.CanManageOutgoingCargo;

        if (!_userContext.HasPermission(requiredPermission))
            return OperationResult<CargoNotificationModel>.Fail(
                "Bu işlem için yetkiniz bulunmamaktadır.");

        // WithIncludes: CargoCompany.Name navigasyon property'si yüklü gelir
        var shipment = await _repository.GetByIdWithIncludesAsync(request.CargoShipmentId);
        if (shipment is null)
            return OperationResult<CargoNotificationModel>.Fail("Kargo kaydı bulunamadı.");

        var model = CargoNotificationBuilder.Build(shipment);

        if (request.NotificationType == NotificationType.WhatsApp)
        {
            model.MessageBody = _whatsAppComposer.Compose(model);
        }
        else if (request.NotificationType == NotificationType.Mail)
        {
            model.Subject     = _mailComposer.ComposeSubject(model);
            model.MessageBody = _mailComposer.Compose(model);
        }
        else
        {
            return OperationResult<CargoNotificationModel>.Fail(
                "Bu bildirim türü henüz desteklenmiyor.");
        }

        return OperationResult<CargoNotificationModel>.Ok(model);
    }
}
