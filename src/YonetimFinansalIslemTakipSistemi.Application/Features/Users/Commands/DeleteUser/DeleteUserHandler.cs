using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Users.Commands.DeleteUser;

public class DeleteUserHandler
{
    private readonly IUserRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;

    public DeleteUserHandler(
        IUserRepository repository,
        IAuditLogService auditLogService,
        IUserContext userContext)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
    }

    public async Task<OperationResult<bool>> HandleAsync(DeleteUserRequest request)
    {
        if (!_userContext.HasPermission(PermissionType.CanManageUsers))
            return OperationResult<bool>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        var user = await _repository.GetByIdAsync(request.Id);
        if (user is null)
            return OperationResult<bool>.Fail("Kullanıcı bulunamadı.");

        // Son aktif kullanıcı silinemez
        if (user.IsActive)
        {
            var all = await _repository.GetAllAsync();
            if (all.Count(x => x.IsActive) <= 1)
                return OperationResult<bool>.Fail("Son aktif kullanıcı silinemez.");
        }

        // Silmeden önce eski değerleri yakala — audit için
        var oldValues = $"Kullanıcı Adı: {user.UserName} | Ad Soyad: {user.FullName}";

        // Soft delete: silinmiş ve pasif olarak işaretlenir
        user.IsDeleted = true;
        user.IsActive  = false;
        user.DeletedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(user);

        // Audit: kullanıcı silindi
        await _auditLogService.WriteAsync(
            AuditAction.UserDeleted,
            _userContext.UserId,
            _userContext.FullName,
            "User", user.Id,
            oldValues, null);

        return OperationResult<bool>.Ok(true);
    }
}
