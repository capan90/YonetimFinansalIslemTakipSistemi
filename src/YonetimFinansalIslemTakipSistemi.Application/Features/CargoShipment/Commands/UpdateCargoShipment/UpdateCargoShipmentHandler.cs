using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.UpdateCargoShipment;

public class UpdateCargoShipmentHandler
{
    private readonly ICargoShipmentRepository _repository;
    private readonly ICargoCompanyRepository _cargoCompanyRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;

    public UpdateCargoShipmentHandler(
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

    public async Task<OperationResult<bool>> HandleAsync(UpdateCargoShipmentRequest request)
    {
        var requiredPermission = request.Direction == CargoShipmentDirection.Incoming
            ? PermissionType.CanManageIncomingCargo
            : PermissionType.CanManageOutgoingCargo;

        if (!_userContext.HasPermission(requiredPermission))
            return OperationResult<bool>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        if (request.ShipmentDate == default)
            return OperationResult<bool>.Fail("Kargo tarihi zorunludur.");

        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity is null)
            return OperationResult<bool>.Fail("Kargo kaydı bulunamadı.");

        var oldDir = entity.Direction == CargoShipmentDirection.Incoming ? "Gelen" : "Giden";
        var oldValues = $"Yön: {oldDir} | Tarih: {entity.ShipmentDate:dd.MM.yyyy} | Durum: {entity.Status}";

        // Takip URL'i yeniden hesapla
        string? trackingUrl = null;
        if (request.CargoCompanyId.HasValue && !string.IsNullOrWhiteSpace(request.TrackingNumber))
        {
            var company = await _cargoCompanyRepository.GetByIdAsync(request.CargoCompanyId.Value);
            if (company is not null && !string.IsNullOrWhiteSpace(company.TrackingUrlTemplate))
                trackingUrl = string.Format(company.TrackingUrlTemplate, request.TrackingNumber.Trim());
        }

        entity.ShipmentNumber     = request.ShipmentNumber?.Trim();
        entity.Direction          = request.Direction;
        entity.ShipmentDate       = DateTime.SpecifyKind(request.ShipmentDate.Date, DateTimeKind.Utc);
        entity.ShipmentTime       = request.ShipmentTime;
        entity.ShipmentType       = request.ShipmentType;
        entity.CargoCompanyId     = request.CargoCompanyId;
        entity.CompanyDirectoryId = request.CompanyDirectoryId;
        entity.SenderName         = request.SenderName?.Trim();
        entity.ReceiverName       = request.ReceiverName?.Trim();
        entity.DeliveredBy        = request.DeliveredBy?.Trim();
        entity.ReceivedBy         = request.ReceivedBy?.Trim();
        entity.VehiclePlate       = request.VehiclePlate?.Trim();
        entity.TrackingNumber     = request.TrackingNumber?.Trim();
        entity.TrackingUrl        = trackingUrl;
        entity.Status             = request.Status;
        entity.NotificationStatus = request.NotificationStatus;
        entity.Notes              = request.Notes?.Trim();
        entity.UpdatedByUserId    = request.UpdatedByUserId;
        entity.UpdatedAt          = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);

        var newDir = entity.Direction == CargoShipmentDirection.Incoming ? "Gelen" : "Giden";
        await _auditLogService.WriteAsync(
            AuditAction.CargoShipmentUpdated,
            _userContext.UserId,
            _userContext.FullName,
            "CargoShipment", entity.Id,
            oldValues, $"Yön: {newDir} | Tarih: {entity.ShipmentDate:dd.MM.yyyy} | Durum: {entity.Status}");

        return OperationResult<bool>.Ok(true);
    }
}
