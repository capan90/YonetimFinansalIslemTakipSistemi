using System.Globalization;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Import;

/// <summary>
/// Finans içe aktarma kolon şeması — kullanıcının gerçek dosyasına göre tasarlandı:
/// GİREN/ÇIKAN iki ayrı tutar kolonu (tam biri dolu olmalı → Giriş/Çıkış + Tutar).
/// NO, AY, BAKİYE gibi türetilmiş kolonlar tolere edilir (yok sayılır).
/// Para Birimi kolonu opsiyoneldir; yoksa/boşsa TL kabul edilir.
/// </summary>
public static class CashImportColumnMap
{
    public enum Column
    {
        Tarih,
        Aciklama,
        Giren,
        Cikan,
        ParaBirimi,
        IlgiliKisi
    }

    public sealed record ColumnDefinition(Column Key, string Header, bool Required, int MaxLength);

    public static readonly IReadOnlyList<ColumnDefinition> Columns =
    [
        new(Column.Tarih,      "Tarih",       Required: true,  MaxLength: 10),
        new(Column.Aciklama,   "Açıklama",    Required: true,  MaxLength: 400),
        new(Column.Giren,      "Giren",       Required: false, MaxLength: 30),
        new(Column.Cikan,      "Çıkan",       Required: false, MaxLength: 30),
        new(Column.ParaBirimi, "Para Birimi", Required: false, MaxLength: 20),
        new(Column.IlgiliKisi, "İlgili Kişi", Required: false, MaxLength: 100),
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

        // GİREN ve ÇIKAN'dan en az biri dosyada bulunmalı — ikisi de yoksa tutar okunamaz
        var missing = Columns.Where(c => c.Required && !indexes.ContainsKey(c.Key))
                             .Select(c => c.Header).ToList();
        if (!indexes.ContainsKey(Column.Giren) && !indexes.ContainsKey(Column.Cikan))
            missing.Add("Giren / Çıkan (en az biri)");

        return new MatchResult
        {
            Indexes = indexes,
            MissingRequired = missing,
            ExtraHeaders = headers.Where((h, i) => !matched.Contains(i) && !string.IsNullOrWhiteSpace(h)).ToList()
        };
    }

    public static ColumnDefinition Definition(Column column) => Columns.First(c => c.Key == column);

    /// <summary>
    /// Tutar parse — biçim sezgisi: virgül varsa Türkçe ("1.234,56"), yoksa invariant
    /// ("10", "10.5" — Excel sayısal hücreleri okuyucudan invariant gelir).
    /// Geçersizse null.
    /// </summary>
    public static decimal? ParseAmount(string? value)
    {
        var s = TextNormalizer.CollapseOrNull(value)?.Replace(" ", "");
        if (s is null) return null;

        var ok = s.Contains(',')
            ? decimal.TryParse(s, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out var amount)
            : decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);

        return ok ? amount : null;
    }

    /// <summary>Türkçe para birimi etiketleri. Boş → TL. Tanınmayan → null (satır hatası).</summary>
    public static CurrencyType? ParseCurrency(string? label)
    {
        var s = CargoImportColumnMap.NormalizeHeader(label);
        return s switch
        {
            "" or "tl" or "try" or "₺" or "türk lirası" => CurrencyType.TRY,
            "usd" or "dolar" or "$"                     => CurrencyType.USD,
            "eur" or "euro" or "€"                      => CurrencyType.EUR,
            _                                            => null
        };
    }
}
