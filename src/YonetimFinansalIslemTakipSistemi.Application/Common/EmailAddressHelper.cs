using System.Text.RegularExpressions;

namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// E-posta adresleri için tek doğrulama/normalize noktası.
/// Alıcı ve CC alanları birden fazla adres kabul ettiğinden ayrıştırma da buradadır —
/// UI, mail rehberi ve SMTP gönderici aynı kuralı kullanır.
/// </summary>
public static partial class EmailAddressHelper
{
    /// <summary>Adresler noktalı virgül, virgül veya boşlukla ayrılabilir.</summary>
    private static readonly char[] Separators = [';', ',', ' ', '\t', '\r', '\n'];

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();

    /// <summary>Kırpar ve küçük harfe çevirir; boşsa null döner. Saklama formatı budur.</summary>
    public static string? Normalize(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        return email.Trim().ToLowerInvariant();
    }

    public static bool IsValid(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            return EmailPattern().IsMatch(email.Trim());
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Serbest metin alanını adres listesine çevirir: normalize eder, tekilleştirir,
    /// geçerli/geçersiz olarak ayırır. Geçersizler kullanıcıya adıyla bildirilir —
    /// "mail gönderilemedi" demek yerine hangi adresin hatalı olduğu söylenir.
    /// </summary>
    public static (IReadOnlyList<string> Valid, IReadOnlyList<string> Invalid) Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return ([], []);

        var valid   = new List<string>();
        var invalid = new List<string>();
        var seen    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in raw.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = part.Trim();
            if (candidate.Length == 0) continue;

            if (!IsValid(candidate))
            {
                // Geçersizler de tekilleştirilir; aynı hata iki kez gösterilmez
                if (seen.Add(candidate)) invalid.Add(candidate);
                continue;
            }

            var normalized = Normalize(candidate)!;
            if (seen.Add(normalized)) valid.Add(normalized);
        }

        return (valid, invalid);
    }

    /// <summary>Adres listesini kullanıcıya gösterilecek/alana yazılacak metne çevirir.</summary>
    public static string Join(IEnumerable<string> addresses) => string.Join("; ", addresses);
}
