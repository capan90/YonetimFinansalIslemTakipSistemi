using System.Globalization;

namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// İsim/adres alanları için text normalize yardımcısı.
/// </summary>
public static class TextNormalizer
{
    private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// Trim + çoklu boşluk temizleme + her kelimenin ilk harfi büyük, devamı küçük (Türkçe farkında).
    /// Boş/null input → string.Empty döner.
    /// </summary>
    public static string TitleCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return string.Join(" ",
            value.Trim()
                 .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                 .Select(w => w.Length == 0 ? w
                     : char.ToUpper(w[0], TrCulture) + w[1..].ToLower(TrCulture)));
    }

    /// <summary>
    /// Trim + çoklu boşluk tekleştirme. Büyük/küçük harf değiştirmez.
    /// Adres satırları ve serbest metin için kullanılır.
    /// </summary>
    public static string CollapseSpaces(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return string.Join(" ", value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Null veya boşsa null döner, değilse Trim() + ToUpperInvariant() uygular.</summary>
    public static string? UpperOrNull(string? value)
    {
        var s = value?.Trim();
        return string.IsNullOrEmpty(s) ? null : s.ToUpperInvariant();
    }

    /// <summary>Null veya boşsa null, değilse TitleCase uygulanmış değer döner.</summary>
    public static string? TitleCaseOrNull(string? value)
    {
        var s = TitleCase(value);
        return s.Length == 0 ? null : s;
    }

    /// <summary>Null veya boşsa null, değilse CollapseSpaces uygulanmış değer döner.</summary>
    public static string? CollapseOrNull(string? value)
    {
        var s = CollapseSpaces(value);
        return s.Length == 0 ? null : s;
    }
}
