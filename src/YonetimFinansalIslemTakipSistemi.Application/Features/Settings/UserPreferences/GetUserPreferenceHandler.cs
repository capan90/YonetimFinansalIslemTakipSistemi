using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Settings.UserPreferences;

/// <summary>
/// Aktif kullanıcının harf tercihini döner. Kayıt yoksa varsayılan: Olduğu Gibi.
/// Login akışı ve ayar ekranı kullanır.
/// </summary>
public class GetUserPreferenceHandler
{
    private readonly IUserPreferenceRepository _repository;
    private readonly IUserContext _userContext;

    public GetUserPreferenceHandler(IUserPreferenceRepository repository, IUserContext userContext)
    {
        _repository  = repository;
        _userContext = userContext;
    }

    public async Task<TextCasePreference> HandleAsync()
    {
        var pref = await _repository.GetByUserIdAsync(_userContext.UserId);
        return pref?.TextCase ?? TextCasePreference.Preserve;
    }
}
