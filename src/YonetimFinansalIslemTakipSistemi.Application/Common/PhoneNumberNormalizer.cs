namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// WhatsApp rehberi için Türkiye telefon numarası normalizasyonu.
/// "0532 123 45 67", "5321234567", "+90 532 123 45 67", "0090 532 123 45 67"
/// yazımlarının tümü aynı numara kabul edilir; kalıcı format: +905321234567.
/// Mükerrer kontrol ve unique index bu normalize değer üzerinden çalışır.
/// </summary>
public static class PhoneNumberNormalizer
{
    /// <summary>
    /// Numarayı +905XXXXXXXXX formatına çevirir. Geçersizse null döner.
    /// WhatsApp mobil numarası hedeflendiği için yerel kısım 5 ile başlamalıdır.
    /// </summary>
    public static string? NormalizeTr(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;

        // 0090... → 90..., +90 zaten rakam filtresiyle 90'a düşer
        if (digits.StartsWith("00"))
            digits = digits[2..];

        // 90 ülke kodu varsa yerel kısma indir
        if (digits.StartsWith("90") && digits.Length == 12)
            digits = digits[2..];

        // 0532... → 532...
        if (digits.StartsWith("0") && digits.Length == 11)
            digits = digits[1..];

        // Yerel kısım: 10 hane ve mobil (5xx) olmalı
        if (digits.Length != 10 || !digits.StartsWith("5"))
            return null;

        return "+90" + digits;
    }
}
