using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

public interface IWhatsAppContactRepository
{
    Task<WhatsAppContact?> GetByIdAsync(Guid id);

    /// <summary>
    /// Normalize telefonla arar. includeDeleted=true soft delete kayıtları da kapsar —
    /// mükerrer kontrol ve geri yükleme akışı için gereklidir.
    /// </summary>
    Task<WhatsAppContact?> GetByPhoneAsync(string normalizedPhone, bool includeDeleted);

    /// <summary>Ad/telefon/firma araması + firma filtresi. Varsayılan yalnızca aktif kayıtlar.</summary>
    Task<IReadOnlyList<WhatsAppContact>> GetListAsync(string? search, string? company, bool includeInactive);

    Task AddAsync(WhatsAppContact entity);
    Task UpdateAsync(WhatsAppContact entity);

    /// <summary>
    /// İçe aktarma mükerrer kontrolü: soft delete dahil TÜM kayıtlar
    /// (silinmiş numara import'ta geri yüklenir — create akışıyla aynı kural).
    /// </summary>
    Task<IReadOnlyList<WhatsAppContact>> GetAllForImportAsync();

    /// <summary>
    /// Toplu içe aktarma: yeni kayıtlar + geri yüklenen (soft delete'ten dönen)
    /// kayıtlar TEK transaction'da kaydedilir (ya hep ya hiç).
    /// </summary>
    Task SaveImportAsync(IReadOnlyList<WhatsAppContact> toAdd, IReadOnlyList<WhatsAppContact> toUpdate);
}
