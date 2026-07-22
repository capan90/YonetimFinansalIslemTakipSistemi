using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Commands.CreateCargoCompany;

public class CreateCargoCompanyHandler
{
    private readonly ICargoCompanyRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;
    private readonly IUserTextNormalizationService _textNormalization;

    public CreateCargoCompanyHandler(
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

    public async Task<OperationResult<CreateCargoCompanyResponse>> HandleAsync(
        CreateCargoCompanyRequest request)
    {
        if (!_userContext.HasPermission(PermissionType.CanManageCargoCompanies))
            return OperationResult<CreateCargoCompanyResponse>.Fail(
                "Bu işlem için yetkiniz bulunmamaktadır.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return OperationResult<CreateCargoCompanyResponse>.Fail("Kargo firması adı zorunludur.");
        if (!string.IsNullOrWhiteSpace(request.Phone) && request.Phone.Trim().Length > 20)
            return OperationResult<CreateCargoCompanyResponse>.Fail("Telefon numarası en fazla 20 karakter olabilir.");
        if (!UrlValidator.IsValidHttpUrlOrEmpty(request.PortalUrl))
            return OperationResult<CreateCargoCompanyResponse>.Fail(
                "Kargo portal bağlantısı geçerli bir http/https adresi olmalıdır.");

        var entity = new Domain.Entities.CargoCompany
        {
            Id                  = Guid.NewGuid(),
            // Firma adı/notlar harf tercihine tabi; URL alanları dönüştürülmez
            Name                = _textNormalization.Normalize(request.Name) ?? string.Empty,
            TrackingUrlTemplate = request.TrackingUrlTemplate?.Trim(),
            Phone               = request.Phone?.Trim(),
            Website             = request.Website?.Trim(),
            PortalUrl           = request.PortalUrl?.Trim(),
            Notes               = _textNormalization.Normalize(request.Notes),
            IsActive            = request.IsActive,
            CreatedByUserId     = request.CreatedByUserId,
            CreatedAt           = DateTime.UtcNow,
            IsDeleted           = false
        };

        await _repository.AddAsync(entity);

        await _auditLogService.WriteAsync(
            AuditAction.CargoCompanyCreated,
            _userContext.UserId,
            _userContext.FullName,
            "CargoCompany", entity.Id,
            null, $"Ad: {entity.Name}");

        return OperationResult<CreateCargoCompanyResponse>.Ok(new CreateCargoCompanyResponse
        {
            Id        = entity.Id,
            Name      = entity.Name,
            CreatedAt = entity.CreatedAt
        });
    }
}
