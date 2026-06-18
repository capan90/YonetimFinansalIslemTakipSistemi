using YonetimFinansalIslemTakipSistemi.Domain.Common;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Domain.Entities;

/// <summary>
/// Sistemdeki finansal işlem kaydını temsil eder.
/// </summary>
public class CashTransaction : BaseEntity
{
    /// <summary>
    /// İşlem tarihi.
    /// </summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// İşlem tipi.
    /// </summary>
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// İşlem para birimi.
    /// </summary>
    public CurrencyType CurrencyType { get; set; }

    /// <summary>
    /// İşlem tutarı.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// İşlem açıklaması.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
