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

    /// <summary>
    /// Verilen yön ve yıl için sıradaki kargo numarasını döner.
    /// Gelen: C-YYYY-0001, Giden: G-YYYY-0001. Silinmiş kayıtlar dahil tüm mevcut numaralar dikkate alınır.
    /// </summary>
    Task<string> GetNextShipmentNumberAsync(CargoShipmentDirection direction, int year);
}
