using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// Kimlik doğrulama sonucunu taşır. Başarılı girişte kullanıcı izinleri de eklenir.
/// </summary>
public sealed record AuthResult(
    bool                         Success,
    Guid?                        UserId,
    string?                      FullName,
    string?                      ErrorMessage,
    IReadOnlySet<PermissionType> Permissions)
{
    public static AuthResult Ok(Guid userId, string fullName, IReadOnlySet<PermissionType> permissions)
        => new(true, userId, fullName, null, permissions);

    public static AuthResult Fail(string error)
        => new(false, null, null, error, new HashSet<PermissionType>());
}
