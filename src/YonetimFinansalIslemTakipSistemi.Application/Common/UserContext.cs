using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// IUserContext (okuma) ve IUserSession (yazma) singleton implementasyonu.
/// IUserContext: handler'lar tarafından okunur.
/// IUserSession: LoginViewModel (SetUser) ve App.xaml.cs (Clear) tarafından yazılır.
/// </summary>
public sealed class UserContext : IUserContext, IUserSession
{
    private HashSet<PermissionType> _permissions = [];

    public Guid   UserId   { get; private set; }
    public string FullName { get; private set; } = string.Empty;

    public TextCasePreference TextCasePreference { get; private set; } = TextCasePreference.Preserve;

    public IReadOnlySet<PermissionType> Permissions => _permissions;

    public bool HasPermission(PermissionType permission)
        => _permissions.Contains(permission);

    public void SetUser(Guid userId, string fullName, IReadOnlySet<PermissionType> permissions)
    {
        UserId       = userId;
        FullName     = fullName;
        _permissions = new HashSet<PermissionType>(permissions);
        // Tercih login akışında ayrıca yüklenir; önceki oturumdan sızmasın
        TextCasePreference = TextCasePreference.Preserve;
    }

    public void SetTextCasePreference(TextCasePreference preference)
        => TextCasePreference = preference;

    /// <summary>
    /// Logout sonrası çağrılır. Kullanıcı bilgisi ve tüm izinler temizlenir.
    /// </summary>
    public void Clear()
    {
        UserId   = Guid.Empty;
        FullName = string.Empty;
        TextCasePreference = TextCasePreference.Preserve;
        _permissions.Clear();
    }
}
