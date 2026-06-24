using YonetimFinansalIslemTakipSistemi.Domain.Common;

namespace YonetimFinansalIslemTakipSistemi.Domain.Entities;

/// <summary>
/// Kargo firması (Aras, MNG, PTT vb.).
/// TrackingUrlTemplate: takip numarası ile birleştirilerek tam URL üretilir.
/// Örnek: "https://kargotakip.aras.com.tr/?code={0}"
/// </summary>
public class CargoCompany : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Takip URL şablonu. {0} yerine TrackingNumber eklenir.
    /// Boş ise takip linki üretilmez.
    /// </summary>
    public string? TrackingUrlTemplate { get; set; }
    public string? Phone   { get; set; }
    public string? Website { get; set; }
    public string? Notes   { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CargoShipment> CargoShipments { get; set; } = [];
}
