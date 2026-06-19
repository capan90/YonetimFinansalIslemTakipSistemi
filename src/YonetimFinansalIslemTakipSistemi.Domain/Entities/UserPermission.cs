using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Domain.Entities;

/// <summary>
/// Kullanıcı-yetki ilişkisi. Birleşik PK (UserId + Permission) ile aynı yetki
/// aynı kullanıcıya birden fazla kez eklenemez.
/// </summary>
public class UserPermission
{
    public Guid           UserId     { get; set; }
    public PermissionType Permission { get; set; }

    public User User { get; set; } = null!;
}
