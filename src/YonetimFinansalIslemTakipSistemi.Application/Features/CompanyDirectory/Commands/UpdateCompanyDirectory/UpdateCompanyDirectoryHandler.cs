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
    private readonly IUserTextNormalizationService _textNormalization;

    public UpdateCompanyDirectoryHandler(
        ICompanyDirectoryRepository repository,
        IAuditLogService auditLogService,
        IUserContext userContext,
        IUserTextNormalizationService textNormalization)
    {
        _repository        = repository;
        _auditLogService   = auditLogService;
        _userContext       = userContext;
        _textNormalization = textNormalization;
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

        // Harf dönüşümü kullanıcı tercihine göre yapılır; telefon/e-posta/posta kodu muaf
        entity.CompanyName   = _textNormalization.Normalize(request.CompanyName) ?? string.Empty;
        entity.ContactPerson = _textNormalization.Normalize(request.ContactPerson);
        entity.AttentionTo   = _textNormalization.Normalize(request.AttentionTo);
        entity.AddressLine   = _textNormalization.Normalize(request.AddressLine) ?? string.Empty;
        entity.District      = _textNormalization.Normalize(request.District);
        entity.City          = _textNormalization.Normalize(request.City);
        entity.PostalCode    = request.PostalCode?.Trim();
        entity.Phone         = request.Phone?.Trim();
        entity.Email         = request.Email?.Trim()?.ToLowerInvariant();
        entity.Notes         = _textNormalization.Normalize(request.Notes);
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
        if (!string.IsNullOrWhiteSpace(r.Phone) && r.Phone.Trim().Length > 20)
            return "Telefon numarası en fazla 20 karakter olabilir.";
        return null;
    }

    private static string Format(string name, string address) =>
        $"Firma: {name} | Adres: {address}";
}
