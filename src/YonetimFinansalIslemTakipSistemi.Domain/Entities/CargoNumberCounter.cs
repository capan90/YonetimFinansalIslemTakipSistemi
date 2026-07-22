using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Domain.Entities;

/// <summary>
/// Gelen/giden kargo numarası sayacı. Yön başına tek satır tutulur.
/// Numara üretimi UPDATE ... RETURNING ile atomik yapılır; kayıt tablosundan
/// bağımsızdır ki silinen (soft delete) numaralar asla yeniden kullanılmasın.
/// BaseEntity DEĞİLDİR: soft delete/audit alanları sayaç için anlamsızdır.
/// </summary>
public class CargoNumberCounter
{
    /// <summary>Primary key — yön başına tek sayaç satırı.</summary>
    public CargoShipmentDirection Direction { get; set; }

    /// <summary>Son kullanılan sıra numarası. Sıradaki numara = LastValue + 1.</summary>
    public long LastValue { get; set; }
}
