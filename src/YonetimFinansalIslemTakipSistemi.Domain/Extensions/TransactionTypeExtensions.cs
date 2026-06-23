using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Domain.Extensions;

/// <summary>
/// TransactionType iş kuralları — merkezi ve test edilebilir.
/// Repository bu kararı vermez; Application katmanı bu metodu çağırır.
/// </summary>
public static class TransactionTypeExtensions
{
    /// <summary>
    /// İşlemin finansal yönünü döndürür.
    /// Giriş → Alacak (Inflow), Çıkış → Borç (Outflow).
    /// </summary>
    public static FinancialDirection GetFinancialDirection(this TransactionType type) => type switch
    {
        TransactionType.Giris => FinancialDirection.Inflow,
        TransactionType.Cikis => FinancialDirection.Outflow,
        _                     => throw new ArgumentOutOfRangeException(
                                     nameof(type), type, "Bilinmeyen işlem türü")
    };
}
