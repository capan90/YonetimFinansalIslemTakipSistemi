using YonetimFinansalIslemTakipSistemi.Domain.Common;

namespace YonetimFinansalIslemTakipSistemi.Domain.Entities;

/// <summary>
/// Genel uygulama ayarları. Key/Value formatında saklanır.
/// Şifreli değerler IsEncrypted=true ile işaretlenir.
/// </summary>
public class ApplicationSetting : BaseEntity
{
    public string  Key         { get; set; } = "";
    public string? Value       { get; set; }
    /// <summary>true ise Value alanı DPAPI ile şifrelenmiştir.</summary>
    public bool    IsEncrypted { get; set; }
}
