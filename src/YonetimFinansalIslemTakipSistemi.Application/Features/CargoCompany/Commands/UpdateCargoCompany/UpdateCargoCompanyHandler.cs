using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Commands.UpdateCargoCompany;

public class UpdateCargoCompanyHandler
{
    private readonly ICargoCompanyRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;
    private readonly IUserTextNormalizationService _textNormalization;

    public UpdateCargoCompanyHandler(
        ICargoCompanyRepository repository,
        IAuditLogService auditLogService,
        IUserContext userContext,
        IUserTextNormalizationService textNormalization)
    {
        _repository        = repository;
        _auditLogService   = auditLogService;
        _userContext       = userContext;
        _textNormalization = textNormalization;
    }

    public async Task<OperationResult<bool>> HandleAsync(UpdateCargoCompanyRequest request)
    {
        if (!_userContext.HasPermission(PermissionType.CanManageCargoCompanies))
            return OperationResult<bool>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return OperationResult<bool>.Fail("Kargo firması adı zorunludur.");
        if (!string.IsNullOrWhiteSpace(request.Phone) && request.Phone.Trim().Length > 20)
            return OperationResult<bool>.Fail("Telefon numarası en fazla 20 karakter olabilir.");
        if (!UrlValidator.IsValidHttpUrlOrEmpty(request.PortalUrl))
            return OperationResult<bool>.Fail(
                "Kargo portal bağlantısı geçerli bir http/https adresi olmalıdır.");

        var entity = await _repository.GetByIdWithTrackingAsync(request.Id);
        if (entity is null)
            return OperationResult<bool>.Fail("Kargo firması bulunamadı.");

        // Portal URL değişikliği audit'te izlenir
        var oldValues = $"Ad: {entity.Name} | Portal: {entity.PortalUrl ?? "-"}";

        // Firma adı/notlar harf tercihine tabi; URL alanları dönüştürülmez
        entity.Name                = _textNormalization.Normalize(request.Name) ?? string.Empty;
        entity.TrackingUrlTemplate = request.TrackingUrlTemplate?.Trim();
        entity.Phone               = request.Phone?.Trim();
        entity.Website             = request.Website?.Trim();
        entity.PortalUrl           = request.PortalUrl?.Trim();
        entity.Notes               = _textNormalization.Normalize(request.Notes);
        entity.IsActive            = request.IsActive;
        entity.UpdatedByUserId     = request.UpdatedByUserId;
        entity.UpdatedAt           = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);

        await _auditLogService.WriteAsync(
            AuditAction.CargoCompanyUpdated,
            _userContext.UserId,
            _userContext.FullName,
            "CargoCompany", entity.Id,
            oldValues, $"Ad: {entity.Name} | Portal: {entity.PortalUrl ?? "-"}");

        return OperationResult<bool>.Ok(true);
    }
}
