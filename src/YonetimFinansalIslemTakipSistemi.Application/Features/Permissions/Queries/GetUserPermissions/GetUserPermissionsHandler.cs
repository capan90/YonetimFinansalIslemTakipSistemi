using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Permissions.Queries.GetUserPermissions;

public class GetUserPermissionsHandler
{
    private readonly IUserPermissionRepository _permissionRepository;
    private readonly IUserRepository           _userRepository;
    private readonly IUserContext              _userContext;

    public GetUserPermissionsHandler(
        IUserPermissionRepository permissionRepository,
        IUserRepository           userRepository,
        IUserContext              userContext)
    {
        _permissionRepository = permissionRepository;
        _userRepository       = userRepository;
        _userContext          = userContext;
    }

    public async Task<OperationResult<IReadOnlySet<PermissionType>>> HandleAsync(
        GetUserPermissionsRequest request)
    {
        if (!_userContext.HasPermission(PermissionType.CanManageUsers))
            return OperationResult<IReadOnlySet<PermissionType>>.Fail(
                "Bu işlem için yetkiniz bulunmamaktadır.");

        var user = await _userRepository.GetByIdAsync(request.TargetUserId);
        if (user is null || user.IsDeleted)
            return OperationResult<IReadOnlySet<PermissionType>>.Fail("Kullanıcı bulunamadı.");

        var perms = await _permissionRepository.GetByUserIdAsync(request.TargetUserId);
        return OperationResult<IReadOnlySet<PermissionType>>.Ok(perms);
    }
}
