using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

public interface ICompanyAttentionContactRepository
{
    Task<List<CompanyAttentionContact>> GetByCompanyAsync(Guid companyDirectoryId);
    Task AddAsync(CompanyAttentionContact contact);
    Task UpdateAsync(CompanyAttentionContact contact);
}
