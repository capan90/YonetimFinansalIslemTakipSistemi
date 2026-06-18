namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// Kimlik doğrulama sonucunu taşır. Pozisyonel record olarak immutable'dır.
/// </summary>
public sealed record AuthResult(bool Success, Guid? UserId, string? FullName, string? ErrorMessage)
{
    public static AuthResult Ok(Guid userId, string fullName)
        => new(true, userId, fullName, null);

    public static AuthResult Fail(string error)
        => new(false, null, null, error);
}
