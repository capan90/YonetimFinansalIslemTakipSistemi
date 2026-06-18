using YonetimFinansalIslemTakipSistemi.Domain.Common;

namespace YonetimFinansalIslemTakipSistemi.Domain.Entities;

/// <summary>
/// Uygulama kullanıcısını temsil eder.
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Kullanıcının görünen adı.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Kullanıcının giriş için kullandığı kullanıcı adı.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Şifre özeti veya hash değeri.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Kullanıcının aktif olup olmadığını gösterir.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
