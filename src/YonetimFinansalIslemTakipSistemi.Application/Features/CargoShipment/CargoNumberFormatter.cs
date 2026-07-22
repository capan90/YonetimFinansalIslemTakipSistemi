using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;

/// <summary>
/// Otomatik kargo numarası formatı: Gelen GLN00001, Giden GDN00001.
/// Sıra değeri cargo_number_counters tablosundan atomik olarak alınır;
/// bu sınıf yalnızca deterministik biçimlendirmeden sorumludur.
/// </summary>
public static class CargoNumberFormatter
{
    public const string IncomingPrefix = "GLN";
    public const string OutgoingPrefix = "GDN";

    public static string Prefix(CargoShipmentDirection direction)
        => direction == CargoShipmentDirection.Incoming ? IncomingPrefix : OutgoingPrefix;

    /// <summary>Örn: (Outgoing, 5) → "GDN00005". 99999 aşılırsa hane sayısı doğal olarak büyür.</summary>
    public static string Format(CargoShipmentDirection direction, long sequence)
        => $"{Prefix(direction)}{sequence:D5}";

    /// <summary>
    /// Yeni format numaranın sıra değerini döner; eski format (G/C-YYYY-NNNN),
    /// boş veya yöne uymayan prefix için null. Sayaç geri alma kontrolünde kullanılır.
    /// </summary>
    public static long? TryParseSequence(string? shipmentNumber, CargoShipmentDirection direction)
    {
        var prefix = Prefix(direction);
        if (string.IsNullOrEmpty(shipmentNumber)
            || !shipmentNumber.StartsWith(prefix, StringComparison.Ordinal))
            return null;

        var digits = shipmentNumber[prefix.Length..];
        return digits.Length > 0 && long.TryParse(digits, out var seq) && seq > 0
            ? seq
            : null;
    }
}
