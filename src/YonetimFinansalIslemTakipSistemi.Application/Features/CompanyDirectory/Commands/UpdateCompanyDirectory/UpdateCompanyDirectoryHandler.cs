using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Commands.UpdateCompanyDirectory;

public class UpdateCompanyDirectoryHandler
{
    private readonly ICompanyDirectoryRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;

    public UpdateCompanyDirectoryHandler(
        ICompanyDirectoryRepository repository,
        IAuditLogService auditLogService,
        IUserContext userContext)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
    }

    public async Task<OperationResult<bool>> HandleAsync(UpdateCompanyDirectoryRequest request)
    {
        if (!_userContext.HasPermission(PermissionType.CanManageCompanyDirectory))
            return OperationResult<bool>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        var error = Validate(request);
        if (error is not null)
            return OperationResult<bool>.Fail(error);

        var entity = await _repository.GetByIdWithTrackingAsync(request.Id);
        if (entity is null)
            return OperationResult<bool>.Fail("Firma rehberi kaydı bulunamadı.");

        var oldValues = Format(entity.CompanyName, entity.AddressLine);

        entity.CompanyName   = request.CompanyName.Trim();
        entity.ContactPerson = request.ContactPerson?.Trim();
        entity.AttentionTo   = request.AttentionTo?.Trim();
        entity.AddressLine   = request.AddressLine.Trim();
        entity.District      = request.District?.Trim();
        entity.City          = request.City?.Trim();
        entity.PostalCode    = request.PostalCode?.Trim();
        entity.Phone         = request.Phone?.Trim();
        entity.Email         = request.Email?.Trim();
        entity.Notes         = request.Notes?.Trim();
        entity.IsActive      = request.IsActive;
        entity.UpdatedByUserId = request.UpdatedByUserId;
        entity.UpdatedAt     = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);

        await _auditLogService.WriteAsync(
            AuditAction.CompanyDirectoryUpdated,
            _userContext.UserId,
            _userContext.FullName,
            "CompanyDirectory", entity.Id,
            oldValues, Format(entity.CompanyName, entity.AddressLine));

        return OperationResult<bool>.Ok(true);
    }

    private static string? Validate(UpdateCompanyDirectoryRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.CompanyName))
            return "Firma adı zorunludur.";
        if (string.IsNullOrWhiteSpace(r.AddressLine))
            return "Adres zorunludur.";
        return null;
    }

    private static string Format(string name, string address) =>
        $"Firma: {name} | Adres: {address}";
}
