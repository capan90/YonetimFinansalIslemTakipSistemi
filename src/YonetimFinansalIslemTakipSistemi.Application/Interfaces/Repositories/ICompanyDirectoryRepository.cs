using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

public interface ICompanyDirectoryRepository
{
    Task<CompanyDirectory?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<CompanyDirectory>> GetAllAsync();
    Task AddAsync(CompanyDirectory entity);
    Task UpdateAsync(CompanyDirectory entity);

    /// <summary>Toplu içe aktarma: TÜM kayıtlar tek transaction'da eklenir (ya hep ya hiç).</summary>
    Task AddRangeAsync(IReadOnlyList<CompanyDirectory> entities);
    Task<CompanyDirectory?> GetByIdWithTrackingAsync(Guid id);
}
