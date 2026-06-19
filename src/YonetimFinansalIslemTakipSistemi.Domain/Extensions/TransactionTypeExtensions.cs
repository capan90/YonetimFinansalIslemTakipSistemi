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
    ///
    /// V1 kuralları:
    ///   Tahsilat     → Giriş  (nakit geldi)
    ///   Ödeme        → Çıkış  (nakit gitti)
    ///   Avans        → Çıkış  (çalışana / tarafa ödendi)
    ///   Özel Harcama → Çıkış  (harcama yapıldı)
    ///   Transfer     → Çıkış  (mevcut kasa tek taraflı çıkış kaydeder;
    ///                          hedef kasa/banka bu sistemin kapsamı dışında)
    ///
    /// Transfer V1 teknik borcu: kasalar arası gerçek iki taraflı hareket
    /// desteklendiğinde bu kural yeniden ele alınacak. Bkz. docs/roadmap.md.
    /// </summary>
    public static FinancialDirection GetFinancialDirection(this TransactionType type) => type switch
    {
        TransactionType.Tahsilat    => FinancialDirection.Inflow,
        TransactionType.Odeme       => FinancialDirection.Outflow,
        TransactionType.Avans       => FinancialDirection.Outflow,
        TransactionType.OzelHarcama => FinancialDirection.Outflow,
        TransactionType.Transfer    => FinancialDirection.Outflow,
        _                           => throw new ArgumentOutOfRangeException(
                                           nameof(type), type, "Bilinmeyen işlem türü")
    };
}
