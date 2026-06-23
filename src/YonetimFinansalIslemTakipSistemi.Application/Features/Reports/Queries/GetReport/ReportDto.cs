using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Reports.Queries.GetReport;

public class ReportDto
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate   { get; init; }

    // Uygulanan filtreler — önizleme/export için taşınır
    public TransactionType? FilterTransactionType { get; init; }
    public CurrencyType?    FilterCurrencyType    { get; init; }
    public string?          FilterDescription     { get; init; }

    /// <summary>Her zaman 3 öğe: TRY, USD, EUR sırasıyla. Veri olmayan para birimi sıfırlarla.</summary>
    public List<CurrencySummaryDto>        CurrencySummaries        { get; init; } = new();

    /// <summary>Her zaman 2 öğe: Giriş ve Çıkış için. Veri olmayan türler sıfırlarla.</summary>
    public List<TransactionTypeSummaryDto> TransactionTypeSummaries { get; init; } = new();

    /// <summary>
    /// ShowTransactionDetails=true ise dolu; null ise detay istenmemiş.
    /// Filtre koşullarına uyan işlemler, tarih bazlı artan sırada listelenir.
    /// </summary>
    public List<TransactionDetailDto>? TransactionDetails { get; init; }
}

public class CurrencySummaryDto
{
    public CurrencyType Currency         { get; init; }
    public string       CurrencyDisplay  { get; init; } = string.Empty;
    public decimal      TotalInflow      { get; init; }
    public decimal      TotalOutflow     { get; init; }
    public decimal      NetBalance       => TotalInflow - TotalOutflow;
    public int          TransactionCount { get; init; }
}

public class TransactionTypeSummaryDto
{
    public TransactionType         TransactionType    { get; init; }
    public string                  TypeDisplay        { get; init; } = string.Empty;

    /// <summary>Her zaman 3 öğe: TRY, USD, EUR sırasıyla.</summary>
    public List<CurrencyAmountDto> AmountsByCurrency  { get; init; } = new();
}

public class CurrencyAmountDto
{
    public CurrencyType Currency        { get; init; }
    public string       CurrencyDisplay { get; init; } = string.Empty;
    public decimal      TotalAmount     { get; init; }
    public int          Count           { get; init; }
}

/// <summary>
/// Satır bazlı işlem detayı. Bakiye, rapor içindeki para birimi bazlı kümülatif toplamdır
/// (tüm zamanlardaki bakiye değil; rapor dönemindeki akış toplamı).
/// </summary>
public class TransactionDetailDto
{
    public DateTime TransactionDate { get; init; }
    public string   Description     { get; init; } = string.Empty;
    public string   TypeDisplay     { get; init; } = string.Empty;
    public string   CurrencyDisplay { get; init; } = string.Empty;
    public decimal  Borc            { get; init; }
    public decimal  Alacak          { get; init; }
    /// <summary>İşlem anındaki para birimi bazlı kümülatif bakiye (raporun başından itibaren).</summary>
    public decimal  Balance         { get; init; }
}
