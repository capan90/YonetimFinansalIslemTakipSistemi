using System.Globalization;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

/// <summary>
/// Kargo içe aktarma kolon şeması — başlık adlarını alanlara bağlayan tek kaynak.
/// Format bağımsızdır (ImportDocument üzerinde çalışır). Eşleme kuralları:
/// başlıklar trim + çoklu boşluk tekleştirme + tr-TR harf duyarsız karşılaştırılır,
/// kolon sırası önemsizdir, tanınmayan kolonlar yok sayılır (tolere edilir).
/// İleride farklı format profilleri gerekirse ikinci bir map örneği tanımlanır;
/// analiz katmanı yalnızca bu sınıfın sözleşmesini bilir.
/// </summary>
public sealed class CargoImportColumnMap
{
    public enum Column
    {
        Tarih,
        Firma,
        KargoFirmasi,
        GonderiTuru,
        Oncelik,
        Gonderen,
        Alici,
        Dikkatine,
        TakipNo,
        AracPlakasi,
        Not
    }

    public sealed record ColumnDefinition(Column Key, string Header, bool Required, int MaxLength);

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// Varsayılan şablon kolonları. MaxLength değerleri entity konfigürasyonundaki
    /// kısıtların aynısıdır (CargoShipmentConfiguration).
    /// </summary>
    public static readonly IReadOnlyList<ColumnDefinition> Columns =
    [
        new(Column.Tarih,        "Tarih",         Required: true,  MaxLength: 10),
        new(Column.Firma,        "Firma",         Required: true,  MaxLength: 200),
        new(Column.KargoFirmasi, "Kargo Firması", Required: false, MaxLength: 200),
        new(Column.GonderiTuru,  "Gönderi Türü",  Required: false, MaxLength: 50),
        new(Column.Oncelik,      "Öncelik",       Required: false, MaxLength: 50),
        new(Column.Gonderen,     "Gönderen",      Required: false, MaxLength: 200),
        new(Column.Alici,        "Alıcı",         Required: false, MaxLength: 200),
        new(Column.Dikkatine,    "Dikkatine",     Required: false, MaxLength: 200),
        new(Column.TakipNo,      "Takip No",      Required: false, MaxLength: 100),
        new(Column.AracPlakasi,  "Araç Plakası",  Required: false, MaxLength: 20),
        new(Column.Not,          "Not",           Required: false, MaxLength: 2000),
    ];

    public sealed class MatchResult
    {
        /// <summary>Kolon → belge içindeki hücre indeksi.</summary>
        public required IReadOnlyDictionary<Column, int> Indexes { get; init; }

        /// <summary>Eksik ZORUNLU kolon başlıkları — varsa içe aktarma başlayamaz.</summary>
        public required IReadOnlyList<string> MissingRequired { get; init; }

        /// <summary>Şemada karşılığı olmayan başlıklar — yok sayılır, bilgi olarak raporlanır.</summary>
        public required IReadOnlyList<string> ExtraHeaders { get; init; }
    }

    /// <summary>Başlık satırını şemayla eşler. Aynı başlık iki kez geçerse ilki kullanılır.</summary>
    public static MatchResult MatchHeaders(IReadOnlyList<string> headers)
    {
        var normalized = headers.Select(NormalizeHeader).ToList();
        var indexes    = new Dictionary<Column, int>();
        var matchedHeaderIndexes = new HashSet<int>();

        foreach (var def in Columns)
        {
            var target = NormalizeHeader(def.Header);
            var index  = normalized.FindIndex(h => h == target);
            if (index >= 0 && !indexes.ContainsKey(def.Key))
            {
                indexes[def.Key] = index;
                matchedHeaderIndexes.Add(index);
            }
        }

        var missing = Columns
            .Where(c => c.Required && !indexes.ContainsKey(c.Key))
            .Select(c => c.Header)
            .ToList();

        var extra = headers
            .Where((h, i) => !matchedHeaderIndexes.Contains(i) && !string.IsNullOrWhiteSpace(h))
            .ToList();

        return new MatchResult { Indexes = indexes, MissingRequired = missing, ExtraHeaders = extra };
    }

    public static ColumnDefinition Definition(Column column) => Columns.First(c => c.Key == column);

    /// <summary>Trim + çoklu boşluk tekleştirme + tr-TR küçük harf ("KARGO  Fırması" == "kargo fırması").</summary>
    public static string NormalizeHeader(string? header)
        => Common.TextNormalizer.CollapseSpaces(header).ToLower(Tr);

    // ── Türkçe etiket → enum eşlemeleri (CargoShipmentEditViewModel ile aynı etiketler) ──

    public static Domain.Enums.CargoShipmentType? ParseShipmentType(string? label) => NormalizeHeader(label) switch
    {
        "evrak"       => Domain.Enums.CargoShipmentType.Document,
        "numune"      => Domain.Enums.CargoShipmentType.Sample,
        "fatura"      => Domain.Enums.CargoShipmentType.Invoice,
        "sözleşme"    => Domain.Enums.CargoShipmentType.Contract,
        "yedek parça" => Domain.Enums.CargoShipmentType.SparePart,
        "diğer"       => Domain.Enums.CargoShipmentType.Other,
        _             => null
    };

    public static Domain.Enums.CargoShipmentPriority? ParsePriority(string? label) => NormalizeHeader(label) switch
    {
        "normal"   => Domain.Enums.CargoShipmentPriority.Normal,
        "orta"     => Domain.Enums.CargoShipmentPriority.Medium,
        "acil"     => Domain.Enums.CargoShipmentPriority.Urgent,
        "çok acil" => Domain.Enums.CargoShipmentPriority.Critical,
        _          => null
    };

    /// <summary>
    /// Tarih parse: önce dd.MM.yyyy (şablon formatı), sonra tr-TR genel formatlar.
    /// Excel okuyucu tarih hücrelerini zaten dd.MM.yyyy metnine çevirir.
    /// </summary>
    public static DateTime? ParseDate(string? value)
    {
        var s = Common.TextNormalizer.CollapseSpaces(value);
        if (s.Length == 0) return null;

        if (DateTime.TryParseExact(s, "dd.MM.yyyy", Tr, DateTimeStyles.None, out var exact))
            return exact;

        return DateTime.TryParse(s, Tr, DateTimeStyles.None, out var general) ? general.Date : null;
    }
}
