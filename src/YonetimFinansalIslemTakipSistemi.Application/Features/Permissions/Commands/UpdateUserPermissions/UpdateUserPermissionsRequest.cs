using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Permissions.Commands.UpdateUserPermissions;

public class UpdateUserPermissionsRequest
{
    public Guid                        TargetUserId { get; set; }
    public IEnumerable<PermissionType> Permissions  { get; set; } = [];
}
