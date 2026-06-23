using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Reports.Queries.GetReport;

public class GetReportQuery
{
    /// <summary>Dahil; null ise açık uç (başlangıçsız aralık).</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Dahil; null ise açık uç (bitişsiz aralık).</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Null → tüm işlem türleri dahil.</summary>
    public TransactionType? TransactionType { get; set; }

    /// <summary>Null → tüm para birimleri dahil.</summary>
    public CurrencyType? CurrencyType { get; set; }

    /// <summary>Null veya boş → açıklama filtresi uygulanmaz. Büyük/küçük harf duyarsız içerir araması.</summary>
    public string? DescriptionContains { get; set; }

    /// <summary>True ise rapor, özet toplamların yanı sıra satır bazlı işlem detaylarını da içerir.</summary>
    public bool ShowTransactionDetails { get; set; }
}
