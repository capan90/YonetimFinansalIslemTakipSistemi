using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Commands.CreateCompanyDirectory;

public class CreateCompanyDirectoryHandler
{
    private readonly ICompanyDirectoryRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;
    private readonly IUserTextNormalizationService _textNormalization;

    public CreateCompanyDirectoryHandler(
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

    public async Task<OperationResult<CreateCompanyDirectoryResponse>> HandleAsync(
        CreateCompanyDirectoryRequest request)
    {
        if (!_userContext.HasPermission(PermissionType.CanManageCompanyDirectory))
            return OperationResult<CreateCompanyDirectoryResponse>.Fail(
                "Bu işlem için yetkiniz bulunmamaktadır.");

        var error = Validate(request);
        if (error is not null)
            return OperationResult<CreateCompanyDirectoryResponse>.Fail(error);

        var entity = new Domain.Entities.CompanyDirectory
        {
            Id            = Guid.NewGuid(),
            // Harf dönüşümü kullanıcı tercihine göre yapılır; telefon/e-posta/posta kodu muaf
            CompanyName   = _textNormalization.Normalize(request.CompanyName) ?? string.Empty,
            ContactPerson = _textNormalization.Normalize(request.ContactPerson),
            AttentionTo   = _textNormalization.Normalize(request.AttentionTo),
            AddressLine   = _textNormalization.Normalize(request.AddressLine) ?? string.Empty,
            District      = _textNormalization.Normalize(request.District),
            City          = _textNormalization.Normalize(request.City),
            PostalCode    = request.PostalCode?.Trim(),
            Phone         = request.Phone?.Trim(),
            Email         = request.Email?.Trim()?.ToLowerInvariant(),
            Notes         = _textNormalization.Normalize(request.Notes),
            IsActive      = request.IsActive,
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt     = DateTime.UtcNow,
            IsDeleted     = false
        };

        await _repository.AddAsync(entity);

        await _auditLogService.WriteAsync(
            AuditAction.CompanyDirectoryCreated,
            _userContext.UserId,
            _userContext.FullName,
            "CompanyDirectory", entity.Id,
            null, Format(entity.CompanyName, entity.AddressLine));

        return OperationResult<CreateCompanyDirectoryResponse>.Ok(new CreateCompanyDirectoryResponse
        {
            Id          = entity.Id,
            CompanyName = entity.CompanyName,
            CreatedAt   = entity.CreatedAt
        });
    }

    private static string? Validate(CreateCompanyDirectoryRequest r)
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
