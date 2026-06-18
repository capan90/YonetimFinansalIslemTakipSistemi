using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// IUserContext'in oturum ömrü boyunca yaşayan singleton implementasyonu.
/// Set() yalnızca LoginViewModel tarafından başarılı girişte bir kez çağrılır.
/// </summary>
public sealed class UserContext : IUserContext
{
    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;

    public void Set(Guid userId, string fullName)
    {
        UserId   = userId;
        FullName = fullName;
    }
}
