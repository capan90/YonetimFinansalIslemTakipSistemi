using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

public interface ICargoShipmentRepository
{
    Task<CargoShipment?> GetByIdAsync(Guid id);
    Task<CargoShipment?> GetByIdWithIncludesAsync(Guid id);
    // NOT: GetByDirectionAsync kaldırıldı — liste ekranı da GetFilteredReportAsync kullanır
    Task AddAsync(CargoShipment entity);
    Task UpdateAsync(CargoShipment entity);

    /// <summary>
    /// Kaydı, yön bazlı atomik sayaçtan üretilen numarayla (GLN00001/GDN00001) tek
    /// transaction içinde ekler. Rollback durumunda numara boşa gitmez; eşzamanlı
    /// eklemelerde mükerrer numara oluşmaz. Entity.ShipmentNumber çağrı sonrası doludur.
    /// </summary>
    Task AddWithAutoNumberAsync(CargoShipment entity);

    /// <summary>
    /// Kaydı soft delete eder; yalnızca yönün EN SON üretilen numarası siliniyorsa
    /// (sayaç = kayıt numarası ve daha yüksek numaralı kayıt yoksa) sayaç aynı
    /// transaction içinde bir geri alınır ve numara sonraki kayıtta yeniden kullanılabilir.
    /// Aradaki silinmiş numaralar asla geri kullanılmaz. Geri alınan sıra değeri,
    /// geri alma olmadıysa null döner. Silme flag'leri (IsDeleted/DeletedAt/DeletedBy)
    /// çağrı öncesi entity üzerinde set edilmiş olmalıdır.
    /// </summary>
    Task<long?> SoftDeleteWithNumberReclaimAsync(CargoShipment entity);

    /// <summary>Silinmemiş tüm kargo kayıtlarını ilişkileriyle döner (Dashboard, Rapor için).</summary>
    Task<IReadOnlyList<CargoShipment>> GetAllActiveAsync();

    /// <summary>En son oluşturulan <paramref name="count"/> kaydı ilişkileriyle döner.</summary>
    Task<IReadOnlyList<CargoShipment>> GetRecentAsync(int count);

    /// <summary>Sunucu tarafı filtreli rapor verisi; metin filtreleme handler'da yapılır.</summary>
    Task<IReadOnlyList<CargoShipment>> GetFilteredReportAsync(
        DateTime?                dateFrom,
        DateTime?                dateTo,
        CargoShipmentDirection?  direction,
        Guid?                    cargoCompanyId,
        CargoShipmentStatus?     status,
        CargoNotificationStatus? notificationStatus,
        CargoShipmentPriority?   priority);
}
