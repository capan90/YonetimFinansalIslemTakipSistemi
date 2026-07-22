namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// İş kayıtlarında saklanan URL alanları için doğrulama.
/// Yalnızca mutlak http/https adresleri kabul edilir; boş değere izin verilir.
/// </summary>
public static class UrlValidator
{
    /// <summary>Boş/null → true (alan opsiyonel). Dolu ise geçerli http/https URL olmalıdır.</summary>
    public static bool IsValidHttpUrlOrEmpty(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;

        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
