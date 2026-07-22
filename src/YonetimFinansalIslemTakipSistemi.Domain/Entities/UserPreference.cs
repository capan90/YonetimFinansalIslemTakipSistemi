using YonetimFinansalIslemTakipSistemi.Domain.Common;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Domain.Entities;

/// <summary>
/// Kullanıcı bazlı uygulama tercihleri. Kullanıcı başına tek kayıt tutulur (UserId unique).
/// Ayrı tablo: users tablosuna dokunmadan yeni tercihlerle genişleyebilir.
/// </summary>
public class UserPreference : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Metin girişlerinin harf dönüşüm tercihi. Varsayılan: Olduğu Gibi.</summary>
    public TextCasePreference TextCase { get; set; } = TextCasePreference.Preserve;
}
