using YonetimFinansalIslemTakipSistemi.Domain.Common;

namespace YonetimFinansalIslemTakipSistemi.Domain.Entities;

/// <summary>
/// Tüm kullanıcıların ortak kullandığı mail rehber kaydı — WhatsApp rehberinin kardeşi.
/// Kargo bildirim maillerinde alıcı/CC adreslerinin her seferinde elle yazılmasını önler.
/// Email alanı küçük harfe normalize edilerek saklanır; mükerrer kontrol ve unique
/// index bu alan üzerinden çalışır.
/// </summary>
public class MailContact : BaseEntity
{
    /// <summary>Ad Soyad / Kayıt Adı.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Normalize e-posta (küçük harf, trim edilmiş). Harf tercihi uygulanmaz.</summary>
    public string Email { get; set; } = string.Empty;

    public string? Company     { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// İş kuralı: true ise mail hazırlama ekranı açıldığında bu adres CC alanına
    /// otomatik eklenir — "her seferinde aynı kişileri CC'ye yazma" ihtiyacını karşılar.
    /// </summary>
    public bool IsDefaultCc { get; set; }

    /// <summary>
    /// Son başarılı gönderimde kullanıldığı an. Öneri listesi buna göre sıralanır;
    /// sık kullanılan adresler üstte kalır. Hiç kullanılmadıysa null.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    public bool IsActive { get; set; } = true;
}
