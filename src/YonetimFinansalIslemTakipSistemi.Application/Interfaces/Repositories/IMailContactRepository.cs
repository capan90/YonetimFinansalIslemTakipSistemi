using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

public interface IMailContactRepository
{
    Task<MailContact?> GetByIdAsync(Guid id);

    /// <summary>
    /// Normalize (küçük harf) e-posta ile arar. includeDeleted=true soft delete
    /// kayıtları da kapsar — mükerrer kontrol ve geri yükleme akışı için gereklidir.
    /// </summary>
    Task<MailContact?> GetByEmailAsync(string normalizedEmail, bool includeDeleted);

    /// <summary>Ad/e-posta/firma araması. Varsayılan yalnızca aktif kayıtlar.</summary>
    Task<IReadOnlyList<MailContact>> GetListAsync(string? search, bool includeInactive);

    /// <summary>Mail ekranı açılışında CC'ye otomatik eklenecek aktif kayıtlar.</summary>
    Task<IReadOnlyList<MailContact>> GetDefaultCcAsync();

    Task AddAsync(MailContact entity);
    Task UpdateAsync(MailContact entity);

    /// <summary>
    /// Başarılı gönderim sonrası kullanılan adreslerin LastUsedAt değerini tek
    /// SaveChanges ile günceller. Rehberde olmayan adresler sessizce atlanır.
    /// </summary>
    Task TouchLastUsedAsync(IReadOnlyCollection<string> normalizedEmails, DateTime usedAtUtc);
}
