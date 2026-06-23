namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCurrentBalances;

/// <summary>
/// Tüm işlemler üzerinden hesaplanan güncel para birimi bakiyeleri.
/// </summary>
public record BalanceSummaryDto(decimal TlBalance, decimal UsdBalance, decimal EurBalance);
