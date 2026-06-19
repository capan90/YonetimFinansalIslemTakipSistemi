using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Oturum açmış kullanıcının kimlik ve yetki bilgisine salt-okuma erişimi.
/// Yazma işlemleri için IUserSession kullanılır.
/// </summary>
public interface IUserContext
{
    Guid   UserId   { get; }
    string FullName { get; }

    IReadOnlySet<PermissionType> Permissions { get; }

    /// <summary>
    /// Belirtilen yetki kullanıcıya atanmışsa true döner.
    /// Handler'larda authorization kontrolü bu metotla yapılır.
    /// </summary>
    bool HasPermission(PermissionType permission);
}
