using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

public interface ICompanyDirectoryRepository
{
    Task<CompanyDirectory?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<CompanyDirectory>> GetAllAsync();
    Task AddAsync(CompanyDirectory entity);
    Task UpdateAsync(CompanyDirectory entity);
    Task<CompanyDirectory?> GetByIdWithTrackingAsync(Guid id);
}
