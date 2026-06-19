using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Reports.Queries.GetReport;

public class ReportDto
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate   { get; init; }

    /// <summary>Her zaman 3 öğe: TRY, USD, EUR sırasıyla. Veri olmayan para birimi sıfırlarla.</summary>
    public List<CurrencySummaryDto>        CurrencySummaries        { get; init; } = new();

    /// <summary>Her zaman 5 öğe: her TransactionType için. Veri olmayan türler sıfırlarla.</summary>
    public List<TransactionTypeSummaryDto> TransactionTypeSummaries { get; init; } = new();
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
