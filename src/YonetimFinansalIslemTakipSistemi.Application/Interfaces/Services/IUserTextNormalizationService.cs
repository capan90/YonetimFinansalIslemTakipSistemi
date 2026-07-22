namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Kullanıcının girdiği anlamlı iş metinlerini, aktif kullanıcının harf tercihine göre
/// kayıt öncesi dönüştürür. İş kuralının tek kaynağı burasıdır; UI yalnızca görsel
/// kolaylık sağlayabilir. Hangi alanların dönüştürüleceği handler seviyesinde açıkça
/// belirtilir — e-posta, telefon, URL, kod/numara alanlarına uygulanmaz.
/// </summary>
public interface IUserTextNormalizationService
{
    /// <summary>
    /// Trim + çoklu boşluk tekleştirme sonrası aktif tercihe göre tr-TR harf dönüşümü uygular.
    /// Preserve: harf değiştirmez. Boş/null giriş null döner.
    /// </summary>
    string? Normalize(string? value);
}
