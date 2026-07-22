using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.CreateCargoShipment;

public class CreateCargoShipmentHandler
{
    private readonly ICargoShipmentRepository    _repository;
    private readonly IAuditLogService            _auditLogService;
    private readonly IUserContext                _userContext;
    private readonly ICargoDashboardCacheService _cache;
    private readonly IUserTextNormalizationService _textNormalization;
    private readonly ISystemLogService           _systemLog;

    public CreateCargoShipmentHandler(
        ICargoShipmentRepository    repository,
        IAuditLogService            auditLogService,
        IUserContext                userContext,
        ICargoDashboardCacheService cache,
        IUserTextNormalizationService textNormalization,
        ISystemLogService           systemLog)
    {
        _repository             = repository;
        _auditLogService        = auditLogService;
        _userContext            = userContext;
        _cache                  = cache;
        _textNormalization      = textNormalization;
        _systemLog              = systemLog;
    }

    public async Task<OperationResult<CreateCargoShipmentResponse>> HandleAsync(
        CreateCargoShipmentRequest request)
    {
        // Yetki: gelen/giden kargo ayrı permission ile korunur
        var requiredPermission = request.Direction == CargoShipmentDirection.Incoming
            ? PermissionType.CanManageIncomingCargo
            : PermissionType.CanManageOutgoingCargo;

        if (!_userContext.HasPermission(requiredPermission))
            return OperationResult<CreateCargoShipmentResponse>.Fail(
                "Bu işlem için yetkiniz bulunmamaktadır.");

        if (request.ShipmentDate == default)
            return OperationResult<CreateCargoShipmentResponse>.Fail("Kargo tarihi zorunludur.");

        // Takip/portal bağlantısı için tek kaynak CargoCompany.PortalUrl'dir;
        // kayıt bazlı TrackingUrl artık üretilmez (eski kayıtlardaki değerler korunur)
        var entity = new Domain.Entities.CargoShipment
        {
            Id                  = Guid.NewGuid(),
            // ShipmentNumber kullanıcıdan alınmaz; AddWithAutoNumberAsync atomik sayaçtan üretir
            Direction           = request.Direction,
            ShipmentDate        = DateTime.SpecifyKind(request.ShipmentDate.Date, DateTimeKind.Utc),
            ShipmentTime        = request.ShipmentTime,
            ShipmentType        = request.ShipmentType,
            Priority            = request.Priority,
            CreatedFrom         = request.CreatedFrom,
            CargoCompanyId      = request.CargoCompanyId,
            CompanyDirectoryId  = request.CompanyDirectoryId,

            // Snapshot: oluşturma anındaki firma bilgileri kalıcı olarak saklanır
            // Snapshot metinleri rehber kaydından kopyalanır (rehberde zaten normalize edilir);
            // dikkatine alanı kullanıcı girişi olduğundan harf tercihine tabidir
            ReceiverCompanyNameSnapshot = request.ReceiverCompanyNameSnapshot?.Trim(),
            ReceiverAddressSnapshot     = request.ReceiverAddressSnapshot?.Trim(),
            ReceiverAttentionSnapshot   = _textNormalization.Normalize(request.ReceiverAttentionSnapshot),
            ReceiverCitySnapshot        = request.ReceiverCitySnapshot?.Trim(),
            ReceiverDistrictSnapshot    = request.ReceiverDistrictSnapshot?.Trim(),
            ReceiverPhoneSnapshot       = request.ReceiverPhoneSnapshot?.Trim(),
            ReceiverEmailSnapshot       = request.ReceiverEmailSnapshot?.Trim(),

            // Harf dönüşümü kullanıcı tercihine göre merkezi serviste yapılır;
            // takip no / URL gibi kod alanlarına uygulanmaz
            SenderName          = _textNormalization.Normalize(request.SenderName),
            ReceiverName        = _textNormalization.Normalize(request.ReceiverName),
            DeliveredBy         = _textNormalization.Normalize(request.DeliveredBy),
            ReceivedBy          = _textNormalization.Normalize(request.ReceivedBy),
            VehiclePlate        = _textNormalization.Normalize(request.VehiclePlate),
            TrackingNumber      = request.TrackingNumber?.Trim(),
            Status              = request.Status,
            NotificationStatus  = CargoNotificationStatus.NotNotified,
            Notes               = _textNormalization.Normalize(request.Notes),
            CreatedByUserId     = request.CreatedByUserId,
            CreatedAt           = DateTime.UtcNow,
            IsDeleted           = false
        };

        // Numara üretimi + insert tek transaction: rollback'te numara boşa gitmez.
        // Audit yalnızca insert başarılı olunca yazılır; hata durumunda gerçek exception
        // System Log'a kaydedilir, kullanıcıya anlaşılır mesaj döner.
        try
        {
            await _repository.AddWithAutoNumberAsync(entity);
        }
        catch (Exception ex)
        {
            await _systemLog.LogErrorAsync(
                "Cargo", "Kargo kaydı oluşturulamadı.", ex, source: nameof(CreateCargoShipmentHandler));

            return OperationResult<CreateCargoShipmentResponse>.Fail(ex is DataStoreException
                ? ex.Message
                : "Kargo kaydı oluşturulamadı. Teknik ayrıntı Sistem Loglarına kaydedildi; sorun sürerse yöneticinize başvurun.");
        }

        var direction = request.Direction == CargoShipmentDirection.Incoming ? "Gelen" : "Giden";
        // Otomatik üretilen numara create audit kaydında yer alır
        await _auditLogService.WriteAsync(
            AuditAction.CargoShipmentCreated,
            _userContext.UserId,
            _userContext.FullName,
            "CargoShipment", entity.Id,
            null, $"Yön: {direction} | No: {entity.ShipmentNumber} | Tarih: {entity.ShipmentDate:dd.MM.yyyy}");

        // Yeni kargo oluşturulunca dashboard cache geçersiz
        _cache.Invalidate();

        return OperationResult<CreateCargoShipmentResponse>.Ok(new CreateCargoShipmentResponse
        {
            Id             = entity.Id,
            ShipmentNumber = entity.ShipmentNumber,
            Direction    = entity.Direction,
            ShipmentDate = entity.ShipmentDate,
            CreatedAt    = entity.CreatedAt
        });
    }
}
