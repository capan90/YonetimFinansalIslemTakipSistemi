using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using static YonetimFinansalIslemTakipSistemi.Application.Common.TextNormalizer;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.CreateCargoShipment;

public class CreateCargoShipmentHandler
{
    private readonly ICargoShipmentRepository _repository;
    private readonly ICargoCompanyRepository _cargoCompanyRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;

    public CreateCargoShipmentHandler(
        ICargoShipmentRepository repository,
        ICargoCompanyRepository cargoCompanyRepository,
        IAuditLogService auditLogService,
        IUserContext userContext)
    {
        _repository            = repository;
        _cargoCompanyRepository = cargoCompanyRepository;
        _auditLogService       = auditLogService;
        _userContext           = userContext;
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

        // ShipmentNumber: boş bırakılırsa yön ve yıla göre otomatik üretilir (G/C-YYYY-0001)
        var shipmentNumber = string.IsNullOrWhiteSpace(request.ShipmentNumber)
            ? await _repository.GetNextShipmentNumberAsync(request.Direction, request.ShipmentDate.Year)
            : request.ShipmentNumber.Trim();

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
            ShipmentNumber      = shipmentNumber,
            Direction           = request.Direction,
            ShipmentDate        = DateTime.SpecifyKind(request.ShipmentDate.Date, DateTimeKind.Utc),
            ShipmentTime        = request.ShipmentTime,
            ShipmentType        = request.ShipmentType,
            Priority            = request.Priority,
            CreatedFrom         = request.CreatedFrom,
            CargoCompanyId      = request.CargoCompanyId,
            CompanyDirectoryId  = request.CompanyDirectoryId,

            // Snapshot: oluşturma anındaki firma bilgileri kalıcı olarak saklanır
            ReceiverCompanyNameSnapshot = request.ReceiverCompanyNameSnapshot?.Trim(),
            ReceiverAddressSnapshot     = request.ReceiverAddressSnapshot?.Trim(),
            ReceiverAttentionSnapshot   = request.ReceiverAttentionSnapshot?.Trim(),
            ReceiverCitySnapshot        = request.ReceiverCitySnapshot?.Trim(),
            ReceiverDistrictSnapshot    = request.ReceiverDistrictSnapshot?.Trim(),
            ReceiverPhoneSnapshot       = request.ReceiverPhoneSnapshot?.Trim(),
            ReceiverEmailSnapshot       = request.ReceiverEmailSnapshot?.Trim(),

            SenderName          = TitleCaseOrNull(request.SenderName),
            ReceiverName        = TitleCaseOrNull(request.ReceiverName),
            DeliveredBy         = TitleCaseOrNull(request.DeliveredBy),
            ReceivedBy          = TitleCaseOrNull(request.ReceivedBy),
            VehiclePlate        = UpperOrNull(request.VehiclePlate),
            TrackingNumber      = request.TrackingNumber?.Trim(),
            TrackingUrl         = trackingUrl,
            Status              = request.Status,
            NotificationStatus  = CargoNotificationStatus.NotNotified,
            Notes               = request.Notes?.Trim(),
            CreatedByUserId     = request.CreatedByUserId,
            CreatedAt           = DateTime.UtcNow,
            IsDeleted           = false
        };

        await _repository.AddAsync(entity);

        var direction = request.Direction == CargoShipmentDirection.Incoming ? "Gelen" : "Giden";
        await _auditLogService.WriteAsync(
            AuditAction.CargoShipmentCreated,
            _userContext.UserId,
            _userContext.FullName,
            "CargoShipment", entity.Id,
            null, $"Yön: {direction} | No: {shipmentNumber} | Tarih: {entity.ShipmentDate:dd.MM.yyyy}");

        return OperationResult<CreateCargoShipmentResponse>.Ok(new CreateCargoShipmentResponse
        {
            Id           = entity.Id,
            Direction    = entity.Direction,
            ShipmentDate = entity.ShipmentDate,
            CreatedAt    = entity.CreatedAt
        });
    }
}
