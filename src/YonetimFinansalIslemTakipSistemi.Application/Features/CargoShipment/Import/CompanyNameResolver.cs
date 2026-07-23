using System.Globalization;
using YonetimFinansalIslemTakipSistemi.Application.Common;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

/// <summary>
/// Serbest metin firma adını kayıtlı firmaya çözümler. Karşılaştırma normalize
/// edilir: trim + çoklu boşluk tekleştirme + tr-TR harf duyarsız.
/// Firma adları benzersiz olmadığından birden fazla eşleşme "muğlak" sayılır.
/// İleride alias (takma ad) tablosu geldiğinde yalnızca bu sınıf genişletilir —
/// analiz ve import handler'ları değişmez.
/// </summary>
public sealed class CompanyNameResolver
{
    public sealed record Entry(Guid Id, string Name, bool IsActive);

    public enum MatchKind { NotFound, Single, Ambiguous, InactiveOnly }

    public sealed record MatchResult(MatchKind Kind, Entry? Match, string? Suggestion);

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private readonly Dictionary<string, List<Entry>> _byNormalizedName;
    private readonly List<(string Normalized, Entry Entry)> _allActive;

    public CompanyNameResolver(IEnumerable<Entry> entries)
    {
        _byNormalizedName = [];
        _allActive        = [];

        foreach (var entry in entries)
        {
            var key = Normalize(entry.Name);
            if (key.Length == 0) continue;

            if (!_byNormalizedName.TryGetValue(key, out var list))
                _byNormalizedName[key] = list = [];
            list.Add(entry);

            if (entry.IsActive)
                _allActive.Add((key, entry));
        }
    }

    public MatchResult Resolve(string? name)
    {
        var key = Normalize(name);
        if (key.Length == 0)
            return new MatchResult(MatchKind.NotFound, null, null);

        if (_byNormalizedName.TryGetValue(key, out var list))
        {
            var active = list.Where(e => e.IsActive).ToList();
            if (active.Count == 1) return new MatchResult(MatchKind.Single, active[0], null);
            if (active.Count > 1)  return new MatchResult(MatchKind.Ambiguous, null, null);
            return new MatchResult(MatchKind.InactiveOnly, null, null);
        }

        // Öneri: normalize ad, sorguyu içeren (veya sorgunun içerdiği) ilk aktif kayıt
        var suggestion = _allActive.FirstOrDefault(e =>
            e.Normalized.Contains(key, StringComparison.Ordinal) ||
            key.Contains(e.Normalized, StringComparison.Ordinal)).Entry;

        return new MatchResult(MatchKind.NotFound, null, suggestion?.Name);
    }

    public static string Normalize(string? value)
        => TextNormalizer.CollapseSpaces(value).ToLower(Tr);
}
