using System.Globalization;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts;

/// <summary>
/// Rehber seçim listeleri için in-memory arama. "m", "mu", "mur" gibi kısmi girişlerde
/// ad, firma veya telefon üzerinden Türkçe farkında ve büyük/küçük duyarsız eşleşir.
/// </summary>
public static class WhatsAppContactSearch
{
    private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static IReadOnlyList<WhatsAppContactDto> Filter(
        IEnumerable<WhatsAppContactDto> contacts, string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return contacts.ToList();

        var t = term.Trim();
        return contacts.Where(c => Matches(c, t)).ToList();
    }

    public static bool Matches(WhatsAppContactDto contact, string term)
        => Contains(contact.FullName, term)
        || Contains(contact.Company, term)
        || Contains(contact.Phone, term);

    private static bool Contains(string? source, string term)
        => !string.IsNullOrEmpty(source)
        && TrCulture.CompareInfo.IndexOf(source, term, CompareOptions.IgnoreCase) >= 0;
}
