using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

public interface IUserPermissionRepository
{
    Task<IReadOnlySet<PermissionType>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Kullanıcının tüm izinlerini transaction içinde günceller:
    /// mevcut izinleri sil → yenilerini ekle.
    /// Yarıda kalırsa kullanıcı izinsiz veya kısmen yetkili bırakılmaz.
    /// </summary>
    Task UpdateAsync(Guid userId, IEnumerable<PermissionType> permissions);

    /// <summary>
    /// Kilitlenme koruması: excludeUserId dışında, belirtilen yetkiye sahip
    /// ve aktif (IsActive=true, IsDeleted=false) başka kullanıcı var mı?
    /// </summary>
    Task<bool> AnyOtherActiveUserHasPermissionAsync(PermissionType permission, Guid excludeUserId);
}
