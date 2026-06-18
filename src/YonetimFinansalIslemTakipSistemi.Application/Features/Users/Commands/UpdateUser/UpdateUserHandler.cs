using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserHandler
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _hasher;

    public UpdateUserHandler(IUserRepository repository, IPasswordHasher hasher)
    {
        _repository = repository;
        _hasher = hasher;
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

        user.FullName   = request.FullName.Trim();
        user.IsActive   = request.IsActive;
        user.UpdatedAt  = DateTime.UtcNow;

        // Şifre yalnızca yeni değer girilmişse güncellenir; boş bırakılırsa mevcut hash korunur
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
            user.PasswordHash = _hasher.Hash(request.NewPassword);

        await _repository.UpdateAsync(user);

        return OperationResult<bool>.Ok(true);
    }
}
