using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Import;

/// <summary>
/// Firma rehberi içe aktarma kolon şeması. Başlık eşleme kuralları kargo şemasıyla
/// aynıdır (trim + tr-TR harf duyarsız, sıra önemsiz, fazladan kolon tolere edilir).
/// Max uzunluklar CompanyDirectoryConfiguration kısıtlarının aynısıdır.
/// </summary>
public static class DirectoryImportColumnMap
{
    public enum Column
    {
        FirmaAdi,
        YetkiliKisi,
        Dikkatine,
        Adres,
        Ilce,
        Il,
        PostaKodu,
        Telefon,
        Eposta,
        Not
    }

    public sealed record ColumnDefinition(Column Key, string Header, bool Required, int MaxLength);

    public static readonly IReadOnlyList<ColumnDefinition> Columns =
    [
        new(Column.FirmaAdi,    "Firma Adı",    Required: true,  MaxLength: 200),
        new(Column.YetkiliKisi, "Yetkili Kişi", Required: false, MaxLength: 200),
        new(Column.Dikkatine,   "Dikkatine",    Required: false, MaxLength: 200),
        new(Column.Adres,       "Adres",        Required: false, MaxLength: 500),
        new(Column.Ilce,        "İlçe",         Required: false, MaxLength: 100),
        new(Column.Il,          "İl",           Required: false, MaxLength: 100),
        new(Column.PostaKodu,   "Posta Kodu",   Required: false, MaxLength: 20),
        new(Column.Telefon,     "Telefon",      Required: false, MaxLength: 50),
        new(Column.Eposta,      "E-posta",      Required: false, MaxLength: 200),
        new(Column.Not,         "Not",          Required: false, MaxLength: 1000),
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
