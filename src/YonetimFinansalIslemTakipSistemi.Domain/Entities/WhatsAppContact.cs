using YonetimFinansalIslemTakipSistemi.Domain.Common;

namespace YonetimFinansalIslemTakipSistemi.Domain.Entities;

/// <summary>
/// Tüm kullanıcıların ortak kullandığı WhatsApp rehber kaydı.
/// Phone alanı her zaman normalize edilmiş +90XXXXXXXXXX formatında saklanır;
/// mükerrer kontrol ve unique index bu alan üzerinden çalışır.
/// </summary>
public class WhatsAppContact : BaseEntity
{
    /// <summary>Ad Soyad / Kayıt Adı.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Normalize telefon: +905321234567. Harf dönüşümü uygulanmaz.</summary>
    public string Phone { get; set; } = string.Empty;

    public string? Company     { get; set; }
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
