namespace YonetimFinansalIslemTakipSistemi.Application.Features.Users;

/// <summary>
/// Parola politikası — tek noktadan yönetilir (Create/Update kullanıcı akışları).
/// V1 kuralı bilinçli olarak sade tutuldu: küçük ekipte kullanılabilirlik ile
/// brute-force direnci dengesi için yalnızca minimum uzunluk zorunlu.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    /// <summary>Geçersizse Türkçe hata mesajı, geçerliyse null döner.</summary>
    public static string? Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "Şifre boş olamaz.";

        if (password.Length < MinLength)
            return $"Şifre en az {MinLength} karakter olmalıdır.";

        return null;
    }
}
