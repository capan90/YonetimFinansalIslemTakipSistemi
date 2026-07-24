using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.Import;

/// <summary>
/// WhatsApp rehberi içe aktarma kolon şeması. Telefon zorunludur ve geçerli bir
/// Türkiye cep numarası olmalıdır (PhoneNumberNormalizer kuralı — create ile aynı).
/// </summary>
public static class WhatsAppImportColumnMap
{
    public enum Column
    {
        AdSoyad,
        Telefon,
        Firma,
        Aciklama
    }

    public sealed record ColumnDefinition(Column Key, string Header, bool Required, int MaxLength);

    public static readonly IReadOnlyList<ColumnDefinition> Columns =
    [
        new(Column.AdSoyad,  "Ad Soyad", Required: true,  MaxLength: 200),
        new(Column.Telefon,  "Telefon",  Required: true,  MaxLength: 50),
        new(Column.Firma,    "Firma",    Required: false, MaxLength: 200),
        new(Column.Aciklama, "Açıklama", Required: false, MaxLength: 1000),
    ];

    public sealed class MatchResult
    {
        public required IReadOnlyDictionary<Column, int> Indexes { get; init; }
        public required IReadOnlyList<string> MissingRequired { get; init; }
        public required IReadOnlyList<string> ExtraHeaders { get; init; }
    }

    public static MatchResult MatchHeaders(IReadOnlyList<string> headers)
    {
        var normalized = headers.Select(CargoImportColumnMap.NormalizeHeader).ToList();
        var indexes    = new Dictionary<Column, int>();
        var matched    = new HashSet<int>();

        foreach (var def in Columns)
        {
            var target = CargoImportColumnMap.NormalizeHeader(def.Header);
            var index  = normalized.FindIndex(h => h == target);
            if (index >= 0 && !indexes.ContainsKey(def.Key))
            {
                indexes[def.Key] = index;
                matched.Add(index);
            }
        }

        return new MatchResult
        {
            Indexes = indexes,
            MissingRequired = Columns.Where(c => c.Required && !indexes.ContainsKey(c.Key))
                                     .Select(c => c.Header).ToList(),
            ExtraHeaders = headers.Where((h, i) => !matched.Contains(i) && !string.IsNullOrWhiteSpace(h)).ToList()
        };
    }

    public static ColumnDefinition Definition(Column column) => Columns.First(c => c.Key == column);
}
