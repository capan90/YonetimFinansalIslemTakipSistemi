using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Queries.GetCompanyDirectoryList;

public class GetCompanyDirectoryListHandler
{
    private readonly ICompanyDirectoryRepository _repository;
    private readonly IUserContext _userContext;

    public GetCompanyDirectoryListHandler(
        ICompanyDirectoryRepository repository,
        IUserContext userContext)
    {
        _repository  = repository;
        _userContext = userContext;
    }

    public async Task<List<CompanyDirectoryDto>> HandleAsync(GetCompanyDirectoryListQuery query)
    {
        if (!_userContext.HasPermission(PermissionType.CanViewCargoModule) &&
            !_userContext.HasPermission(PermissionType.CanManageCompanyDirectory))
            return [];

        var all = await _repository.GetAllAsync();

        IEnumerable<Domain.Entities.CompanyDirectory> filtered = all;

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            filtered = filtered.Where(x =>
                x.CompanyName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                (x.ContactPerson != null && x.ContactPerson.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                (x.City != null && x.City.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                (x.Phone != null && x.Phone.Contains(kw, StringComparison.OrdinalIgnoreCase)));
        }

        if (query.IsActive.HasValue)
            filtered = filtered.Where(x => x.IsActive == query.IsActive.Value);

        return filtered
            .OrderBy(x => x.CompanyName)
            .Select(x => new CompanyDirectoryDto
            {
                Id            = x.Id,
                CompanyName   = x.CompanyName,
                ContactPerson = x.ContactPerson,
                AttentionTo   = x.AttentionTo,
                AddressLine   = x.AddressLine,
                District      = x.District,
                City          = x.City,
                PostalCode    = x.PostalCode,
                Phone         = x.Phone,
                Email         = x.Email,
                Notes         = x.Notes,
                IsActive      = x.IsActive,
                CreatedAt     = x.CreatedAt
            })
            .ToList();
    }
}
