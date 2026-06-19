using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Permissions.Commands.UpdateUserPermissions;

public class UpdateUserPermissionsHandler
{
    private readonly IUserPermissionRepository _permissionRepository;
    private readonly IUserRepository           _userRepository;
    private readonly IAuditLogService          _auditLogService;
    private readonly IUserContext              _userContext;

    public UpdateUserPermissionsHandler(
        IUserPermissionRepository permissionRepository,
        IUserRepository           userRepository,
        IAuditLogService          auditLogService,
        IUserContext              userContext)
    {
        _permissionRepository = permissionRepository;
        _userRepository       = userRepository;
        _auditLogService      = auditLogService;
        _userContext          = userContext;
    }

    public async Task<OperationResult<UpdateUserPermissionsResponse>> HandleAsync(
        UpdateUserPermissionsRequest request)
    {
        // Handler seviyesi yetki kontrolü — UI gizlemesi yedek; asıl kontrol burada
        if (!_userContext.HasPermission(PermissionType.CanManageUsers))
            return OperationResult<UpdateUserPermissionsResponse>.Fail(
                "Bu işlem için yetkiniz bulunmamaktadır.");

        var user = await _userRepository.GetByIdAsync(request.TargetUserId);
        if (user is null || user.IsDeleted)
            return OperationResult<UpdateUserPermissionsResponse>.Fail("Kullanıcı bulunamadı.");

        if (!user.IsActive)
            return OperationResult<UpdateUserPermissionsResponse>.Fail(
                "Pasif kullanıcılar için yetki güncellenemez.");

        var newPerms = new HashSet<PermissionType>(request.Permissions);

        // Kilitlenme koruması: CanManageUsers kaldırılıyorsa, sistemde başka yetkili olmalı
        if (!newPerms.Contains(PermissionType.CanManageUsers))
        {
            var hasOtherAdmin = await _permissionRepository
                .AnyOtherActiveUserHasPermissionAsync(PermissionType.CanManageUsers, request.TargetUserId);
            if (!hasOtherAdmin)
                return OperationResult<UpdateUserPermissionsResponse>.Fail(
                    "Sistemde yönetim yetkisine sahip en az bir kullanıcı olmalıdır. " +
                    "Bu yetkiyi kaldırmadan önce başka bir kullanıcıya atayın.");
        }

        // Audit için mevcut izinleri kaydet
        var oldPerms  = await _permissionRepository.GetByUserIdAsync(request.TargetUserId);
        var oldValues = FormatPermissions(oldPerms);
        var newValues = FormatPermissions(newPerms);

        // Transaction: mevcut izinleri sil → yenilerini ekle
        await _permissionRepository.UpdateAsync(request.TargetUserId, newPerms);

        // Audit: yetki güncellendi — işlemi yapan + etkilenen kullanıcı bilgisi
        await _auditLogService.WriteAsync(
            AuditAction.PermissionUpdated,
            _userContext.UserId,
            _userContext.FullName,
            "UserPermission", request.TargetUserId,
            oldValues.Length > 0 ? oldValues : null,
            newValues.Length > 0 ? newValues : null);

        return OperationResult<UpdateUserPermissionsResponse>.Ok(new UpdateUserPermissionsResponse
        {
            // UI katmanı bu bayrağa göre session izinlerini yeniler
            SelfPermissionsUpdated = request.TargetUserId == _userContext.UserId,
            NewPermissions         = newPerms
        });
    }

    private static string FormatPermissions(IReadOnlySet<PermissionType> perms)
    {
        if (perms.Count == 0) return "Yetki yok";
        return string.Join(" | ", perms.Order().Select(p => p switch
        {
            PermissionType.CanManageUsers       => "Kullanıcı Yönetimi",
            PermissionType.CanViewAuditLog      => "Denetim Günlüğü",
            PermissionType.CanCreateTransaction => "İşlem Oluşturma",
            PermissionType.CanEditTransaction   => "İşlem Düzenleme",
            PermissionType.CanDeleteTransaction => "İşlem Silme",
            _                                   => p.ToString()
        }));
    }
}
