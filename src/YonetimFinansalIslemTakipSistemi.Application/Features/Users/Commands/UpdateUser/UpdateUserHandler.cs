using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserHandler
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;

    public UpdateUserHandler(
        IUserRepository repository,
        IPasswordHasher hasher,
        IAuditLogService auditLogService,
        IUserContext userContext)
    {
        _repository      = repository;
        _hasher          = hasher;
        _auditLogService = auditLogService;
        _userContext     = userContext;
    }

    public async Task<OperationResult<bool>> HandleAsync(UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return OperationResult<bool>.Fail("Ad Soyad boş olamaz.");

        var user = await _repository.GetByIdAsync(request.Id);
        if (user is null)
            return OperationResult<bool>.Fail("Kullanıcı bulunamadı.");

        // Son aktif kullanıcı pasifleştirilemez
        if (user.IsActive && !request.IsActive)
        {
            var all = await _repository.GetAllAsync();
            if (all.Count(x => x.IsActive) <= 1)
                return OperationResult<bool>.Fail("Son aktif kullanıcı pasifleştirilemez.");
        }

        // Mutation öncesi alan değerlerini sakla — diff için
        var prevFullName = user.FullName;
        var prevIsActive = user.IsActive;

        user.FullName  = request.FullName.Trim();
        user.IsActive  = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        // Şifre yalnızca yeni değer girilmişse güncellenir; boş bırakılırsa mevcut hash korunur
        var passwordChanged = !string.IsNullOrWhiteSpace(request.NewPassword);
        if (passwordChanged)
            user.PasswordHash = _hasher.Hash(request.NewPassword!);

        await _repository.UpdateAsync(user);

        // Yalnızca değişen alanları audit'e yaz
        var oldParts = new List<string>();
        var newParts = new List<string>();

        if (prevFullName != user.FullName)
        {
            oldParts.Add($"Ad Soyad: {prevFullName}");
            newParts.Add($"Ad Soyad: {user.FullName}");
        }
        if (prevIsActive != user.IsActive)
        {
            oldParts.Add($"Durum: {(prevIsActive ? "Aktif" : "Pasif")}");
            newParts.Add($"Durum: {(user.IsActive ? "Aktif" : "Pasif")}");
        }
        if (passwordChanged)
            newParts.Add("Şifre: güncellendi");

        await _auditLogService.WriteAsync(
            AuditAction.UserUpdated,
            _userContext.UserId,
            _userContext.FullName,
            "User", user.Id,
            oldParts.Count > 0 ? string.Join(" | ", oldParts) : null,
            newParts.Count > 0 ? string.Join(" | ", newParts) : null);

        return OperationResult<bool>.Ok(true);
    }
}
