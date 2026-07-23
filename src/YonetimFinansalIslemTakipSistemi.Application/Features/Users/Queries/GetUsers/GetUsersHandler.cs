using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Users.Queries.GetUsers;

public class GetUsersHandler
{
    private readonly IUserRepository _repository;
    private readonly IUserContext _userContext;

    public GetUsersHandler(IUserRepository repository, IUserContext userContext)
    {
        _repository  = repository;
        _userContext = userContext;
    }

    public async Task<List<UserDto>> HandleAsync(GetUsersQuery query)
    {
        // Defense-in-depth: kullanıcı listesi yalnızca kullanıcı/yetki yönetimi
        // ekranlarında kullanılır — UI gizlemesine ek olarak handler'da da korunur
        if (!_userContext.HasPermission(PermissionType.CanManageUsers))
            return [];

        var users = await _repository.GetAllAsync();
        return users.Select(u => new UserDto
        {
            Id        = u.Id,
            FullName  = u.FullName,
            UserName  = u.UserName,
            IsActive  = u.IsActive,
            CreatedAt = u.CreatedAt
        }).ToList();
    }
}
