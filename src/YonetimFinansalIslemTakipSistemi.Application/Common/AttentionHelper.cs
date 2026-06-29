namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// Kargo dikkatine satırı için Türkçe sesli uyumu eki hesaplar.
/// </summary>
public static class AttentionHelper
{
    private static readonly char[] BackVowels    = ['a', 'ı', 'o', 'u'];
    private static readonly char[] FrontVowels   = ['e', 'i', 'ö', 'ü'];
    private static readonly char[] AllVowels     = ['a', 'e', 'ı', 'i', 'o', 'ö', 'u', 'ü'];

    /// <summary>
    /// "İlgili: {name} Dikkatine" formatı — firma kartı için.
    /// Eğer name zaten "dikkatine" ile bitiyorsa sadece "İlgili: {name}" döner.
    /// Boşsa boş string döner (satır gizlenecek).
    /// </summary>
    public static string FormatAttentionDisplay(string? attentionName)
    {
        var name = attentionName?.Trim();
        if (string.IsNullOrEmpty(name)) return string.Empty;

        // Kullanıcı zaten "Dikkatine" yazmışsa tekrar ekleme
        if (name.EndsWith("dikkatine", StringComparison.OrdinalIgnoreCase))
            return $"İlgili: {name}";

        return $"İlgili: {name} Dikkatine";
    }

    /// <summary>
    /// "Muhattap: {name}'ın/in Dikkatine" veya "Muhattap: -" (boş isim için).
    /// </summary>
    public static string FormatAttentionLine(string? attentionName)
    {
        var name = attentionName?.Trim();
        if (string.IsNullOrEmpty(name))
            return "Muhattap: -";

        var suffix = GetSuffix(name);
        return $"Muhattap: {name}'{suffix} Dikkatine";
    }

    private static string GetSuffix(string name)
    {
        var lower = name.ToLowerInvariant();
        for (var i = lower.Length - 1; i >= 0; i--)
        {
            var ch = lower[i];
            if (BackVowels.Contains(ch))  return "ın";
            if (FrontVowels.Contains(ch)) return "in";
        }
        return "in"; // sesli bulunamadı
    }
}
