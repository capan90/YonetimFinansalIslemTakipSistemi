using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

public class UserGridLayoutService : IUserGridLayoutService
{
    private readonly IUserGridLayoutRepository _repository;

    public UserGridLayoutService(IUserGridLayoutRepository repository)
    {
        _repository = repository;
    }

    public Task<string?> GetLayoutAsync(Guid userId, string screenKey)
        => _repository.GetLayoutJsonAsync(userId, screenKey);

    public Task SaveLayoutAsync(Guid userId, string screenKey, string layoutJson)
        => _repository.SaveLayoutJsonAsync(userId, screenKey, layoutJson);

    public Task DeleteLayoutAsync(Guid userId, string screenKey)
        => _repository.DeleteLayoutAsync(userId, screenKey);
}
