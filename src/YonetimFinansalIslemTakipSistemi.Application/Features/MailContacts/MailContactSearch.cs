using System.Globalization;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts;

/// <summary>
/// Rehber seçim listeleri için in-memory arama — WhatsAppContactSearch ile aynı desen.
/// "m", "mu", "mur" gibi kısmi girişlerde ad, firma veya adres üzerinden
/// Türkçe farkında ve büyük/küçük duyarsız eşleşir.
/// </summary>
public static class MailContactSearch
{
    private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static IReadOnlyList<MailContactDto> Filter(
        IEnumerable<MailContactDto> contacts, string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return contacts.ToList();

        var t = term.Trim();
        return contacts.Where(c => Matches(c, t)).ToList();
    }

    public static bool Matches(MailContactDto contact, string term)
        => Contains(contact.FullName, term)
        || Contains(contact.Company, term)
        || Contains(contact.Email, term);

    private static bool Contains(string? source, string term)
        => !string.IsNullOrEmpty(source)
        && TrCulture.CompareInfo.IndexOf(source, term, CompareOptions.IgnoreCase) >= 0;
}
