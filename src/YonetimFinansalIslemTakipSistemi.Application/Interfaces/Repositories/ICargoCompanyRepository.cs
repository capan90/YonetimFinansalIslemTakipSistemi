using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

public interface ICargoCompanyRepository
{
    Task<CargoCompany?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<CargoCompany>> GetAllAsync();
    Task AddAsync(CargoCompany entity);
    Task UpdateAsync(CargoCompany entity);
    Task<CargoCompany?> GetByIdWithTrackingAsync(Guid id);
}
