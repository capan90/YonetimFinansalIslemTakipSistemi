using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Users.Queries.GetUsers;

public class GetUsersHandler
{
    private readonly IUserRepository _repository;

    public GetUsersHandler(IUserRepository repository) => _repository = repository;

    public async Task<List<UserDto>> HandleAsync(GetUsersQuery query)
    {
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
