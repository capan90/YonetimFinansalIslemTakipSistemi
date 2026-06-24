using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

public interface ICargoShipmentRepository
{
    Task<CargoShipment?> GetByIdAsync(Guid id);
    Task<CargoShipment?> GetByIdWithIncludesAsync(Guid id);
    Task<IReadOnlyList<CargoShipment>> GetByDirectionAsync(CargoShipmentDirection direction);
    Task AddAsync(CargoShipment entity);
    Task UpdateAsync(CargoShipment entity);
}
