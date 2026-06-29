using YonetimFinansalIslemTakipSistemi.Domain.Common;

namespace YonetimFinansalIslemTakipSistemi.Domain.Entities;

/// <summary>
/// Bir firmaya ait dikkatine kişileri tutar.
/// Kargo formunda kullanıcıya önceki isimler ComboBox ile sunulur.
/// </summary>
public class CompanyAttentionContact : BaseEntity
{
    public Guid   CompanyDirectoryId { get; set; }
    public string Name               { get; set; } = string.Empty;
    public DateTime LastUsedAt       { get; set; }

    public CompanyDirectory CompanyDirectory { get; set; } = null!;
}
