using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using static YonetimFinansalIslemTakipSistemi.Application.Common.TextNormalizer;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Commands.CreateCompanyDirectory;

public class CreateCompanyDirectoryHandler
{
    private readonly ICompanyDirectoryRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;

    public CreateCompanyDirectoryHandler(
        ICompanyDirectoryRepository repository,
        IAuditLogService auditLogService,
        IUserContext userContext)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
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
            CompanyName   = TitleCase(request.CompanyName),
            ContactPerson = TitleCaseOrNull(request.ContactPerson),
            AttentionTo   = TitleCaseOrNull(request.AttentionTo),
            AddressLine   = CollapseSpaces(request.AddressLine),
            District      = TitleCaseOrNull(request.District),
            City          = TitleCaseOrNull(request.City),
            PostalCode    = request.PostalCode?.Trim(),
            Phone         = request.Phone?.Trim(),
            Email         = request.Email?.Trim()?.ToLowerInvariant(),
            Notes         = request.Notes?.Trim(),
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
