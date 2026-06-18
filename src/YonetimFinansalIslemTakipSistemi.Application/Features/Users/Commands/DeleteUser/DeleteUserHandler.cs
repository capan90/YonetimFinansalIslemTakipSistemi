using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Users.Commands.DeleteUser;

public class DeleteUserHandler
{
    private readonly IUserRepository _repository;

    public DeleteUserHandler(IUserRepository repository) => _repository = repository;

    public async Task<OperationResult<bool>> HandleAsync(DeleteUserRequest request)
    {
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

        // Soft delete: silinmiş ve pasif olarak işaretlenir
        user.IsDeleted = true;
        user.IsActive  = false;
        user.DeletedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(user);

        return OperationResult<bool>.Ok(true);
    }
}
