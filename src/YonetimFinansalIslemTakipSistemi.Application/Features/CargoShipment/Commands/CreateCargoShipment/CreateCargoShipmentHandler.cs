using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.CreateCargoShipment;

public class CreateCargoShipmentHandler
{
    private readonly ICargoShipmentRepository    _repository;
    private readonly ICargoCompanyRepository     _cargoCompanyRepository;
    private readonly IAuditLogService            _auditLogService;
    private readonly IUserContext                _userContext;
    private readonly ICargoDashboardCacheService _cache;
    private readonly IUserTextNormalizationService _textNormalization;

    public CreateCargoShipmentHandler(
        ICargoShipmentRepository    repository,
        ICargoCompanyRepository     cargoCompanyRepository,
        IAuditLogService            auditLogService,
        IUserContext                userContext,
        ICargoDashboardCacheService cache,
        IUserTextNormalizationService textNormalization)
    {
        _repository             = repository;
        _cargoCompanyRepository = cargoCompanyRepository;
        _auditLogService        = auditLogService;
        _userContext            = userContext;
        _cache                  = cache;
        _textNormalization      = textNormalization;
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

        // Manuel URL varsa doğrudan kullan; boşsa şablon + takip numarasından üret
        string? trackingUrl = string.IsNullOrWhiteSpace(request.TrackingUrl) ? null : request.TrackingUrl.Trim();
        if (trackingUrl is null && request.CargoCompanyId.HasValue && !string.IsNullOrWhiteSpace(request.TrackingNumber))
        {
            var company = await _cargoCompanyRepository.GetByIdAsync(request.CargoCompanyId.Value);
            if (company is not null && !string.IsNullOrWhiteSpace(company.TrackingUrlTemplate))
                trackingUrl = string.Format(company.TrackingUrlTemplate, request.TrackingNumber.Trim());
        }

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
            TrackingUrl         = trackingUrl,
            Status              = request.Status,
            NotificationStatus  = CargoNotificationStatus.NotNotified,
            Notes               = _textNormalization.Normalize(request.Notes),
            CreatedByUserId     = request.CreatedByUserId,
            CreatedAt           = DateTime.UtcNow,
            IsDeleted           = false
        };

        // Numara üretimi + insert tek transaction: rollback'te numara boşa gitmez
        await _repository.AddWithAutoNumberAsync(entity);

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
            Id           = entity.Id,
            Direction    = entity.Direction,
            ShipmentDate = entity.ShipmentDate,
            CreatedAt    = entity.CreatedAt
        });
    }
}
