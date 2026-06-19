using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Permissions.Commands.UpdateUserPermissions;

public class UpdateUserPermissionsResponse
{
    /// <summary>
    /// True ise güncellenen kullanıcı mevcut oturum sahibiyle aynıdır.
    /// UI katmanı bu durumda IUserSession.SetUser() çağırarak session izinlerini yenilemelidir.
    /// </summary>
    public bool SelfPermissionsUpdated { get; set; }

    public IReadOnlySet<PermissionType> NewPermissions { get; set; } = new HashSet<PermissionType>();
}
