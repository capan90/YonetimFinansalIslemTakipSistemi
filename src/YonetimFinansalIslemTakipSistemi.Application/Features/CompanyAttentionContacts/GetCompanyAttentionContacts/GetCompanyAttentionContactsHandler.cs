using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CompanyAttentionContacts.GetCompanyAttentionContacts;

public class GetCompanyAttentionContactsHandler
{
    private readonly ICompanyAttentionContactRepository _repository;

    public GetCompanyAttentionContactsHandler(ICompanyAttentionContactRepository repository)
        => _repository = repository;

    public async Task<List<CompanyAttentionContactDto>> HandleAsync(GetCompanyAttentionContactsQuery query)
    {
        var contacts = await _repository.GetByCompanyAsync(query.CompanyDirectoryId);
        return contacts
            .OrderByDescending(c => c.LastUsedAt)
            .Select(c => new CompanyAttentionContactDto(c.Id, c.Name, c.LastUsedAt))
            .ToList();
    }
}
