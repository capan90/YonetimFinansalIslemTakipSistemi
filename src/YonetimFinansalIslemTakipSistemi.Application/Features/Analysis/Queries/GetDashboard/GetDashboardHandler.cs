using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Domain.Extensions;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Analysis.Queries.GetDashboard;

public class GetDashboardHandler
{
    private readonly ICashTransactionRepository _repository;
    private readonly IUserContext               _userContext;

    public GetDashboardHandler(ICashTransactionRepository repository, IUserContext userContext)
    {
        _repository  = repository;
        _userContext = userContext;
    }

    public async Task<OperationResult<DashboardDto>> HandleAsync(GetDashboardQuery query)
    {
        // Yetki kontrolü — rapor yetkisiyle aynı kural
        if (!_userContext.HasPermission(PermissionType.CanViewReports))
            return OperationResult<DashboardDto>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        if (query.StartDate.HasValue && query.EndDate.HasValue
            && query.StartDate.Value.Date > query.EndDate.Value.Date)
            return OperationResult<DashboardDto>.Fail("Başlangıç tarihi bitiş tarihinden büyük olamaz.");

        // UTC yarı-açık aralık — Report handler ile aynı kural
        DateTime? startUtc = query.StartDate.HasValue
            ? DateTime.SpecifyKind(query.StartDate.Value.Date, DateTimeKind.Utc)
            : null;
        DateTime? endExclusiveUtc = query.EndDate.HasValue
            ? DateTime.SpecifyKind(query.EndDate.Value.Date.AddDays(1), DateTimeKind.Utc)
            : null;

        var description = string.IsNullOrWhiteSpace(query.DescriptionContains)
            ? null
            : query.DescriptionContains.Trim();

        // Aggregate sorgusu — summary cards ve currency cards için
        var aggregates = await _repository.GetReportDataAsync(
            startUtc, endExclusiveUtc,
            query.TransactionType,
            query.CurrencyType,
            description);

        // Tüm detay satırları — trend, max ve son işlemler için in-memory kullanılır
        var allRows = await _repository.GetFilteredForReportDetailAsync(
            startUtc, endExclusiveUtc,
            query.TransactionType,
            query.CurrencyType,
            description);

        // ── Üst özet kartları ──────────────────────────────────────────────────
        var totalAlacak = aggregates
            .Where(x => x.TransactionType.GetFinancialDirection() == FinancialDirection.Inflow)
            .Sum(x => x.TotalAmount);
        var totalBorc = aggregates
            .Where(x => x.TransactionType.GetFinancialDirection() == FinancialDirection.Outflow)
            .Sum(x => x.TotalAmount);
        var totalCount = aggregates.Sum(x => x.Count);

        var inflowRows  = allRows.Where(x => x.TransactionType.GetFinancialDirection() == FinancialDirection.Inflow).ToList();
        var outflowRows = allRows.Where(x => x.TransactionType.GetFinancialDirection() == FinancialDirection.Outflow).ToList();

        var maxGiris = inflowRows.Count  > 0 ? inflowRows.Max(x => x.Amount)  : 0m;
        var maxCikis = outflowRows.Count > 0 ? outflowRows.Max(x => x.Amount) : 0m;

        // ── Para birimi kartları ───────────────────────────────────────────────
        var currencies = query.CurrencyType.HasValue
            ? new[] { query.CurrencyType.Value }
            : Enum.GetValues<CurrencyType>();

        var currencyCards = currencies.Select(currency =>
        {
            var rows    = aggregates.Where(x => x.Currency == currency).ToList();
            var alacak  = rows.Where(x => x.TransactionType.GetFinancialDirection() == FinancialDirection.Inflow).Sum(x => x.TotalAmount);
            var borc    = rows.Where(x => x.TransactionType.GetFinancialDirection() == FinancialDirection.Outflow).Sum(x => x.TotalAmount);
            var count   = rows.Sum(x => x.Count);
            return new DashboardCurrencyCardDto(DisplayCurrency(currency), alacak, borc, alacak - borc, count);
        }).ToList();

        // ── Günlük trend ───────────────────────────────────────────────────────
        var dailyTrend = allRows
            .GroupBy(x => x.TransactionDate.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var gAlacak = g.Where(x => x.TransactionType.GetFinancialDirection() == FinancialDirection.Inflow).Sum(x => x.Amount);
                var gBorc   = g.Where(x => x.TransactionType.GetFinancialDirection() == FinancialDirection.Outflow).Sum(x => x.Amount);
                return new DailyTrendDto(
                    DateDisplay: g.Key.ToString("dd.MM.yyyy"),
                    TotalAlacak: gAlacak,
                    TotalBorc:   gBorc,
                    NetFark:     gAlacak - gBorc);
            })
            .ToList();

        // ── Son 20 işlem ───────────────────────────────────────────────────────
        var recent = allRows
            .OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.CreatedAt)
            .Take(20)
            .Select(x =>
            {
                var isInflow = x.TransactionType.GetFinancialDirection() == FinancialDirection.Inflow;
                return new RecentTransactionDto(
                    DateDisplay:     x.TransactionDate.ToString("dd.MM.yyyy"),
                    Description:     x.Description ?? string.Empty,
                    TypeDisplay:     DisplayType(x.TransactionType),
                    CurrencyDisplay: DisplayCurrency(x.CurrencyType),
                    Borc:            isInflow ? 0m       : x.Amount,
                    Alacak:          isInflow ? x.Amount : 0m);
            })
            .ToList();

        var dto = new DashboardDto
        {
            TotalAlacak       = totalAlacak,
            TotalBorc         = totalBorc,
            NetFark           = totalAlacak - totalBorc,
            TotalCount        = totalCount,
            MaxGiris          = maxGiris,
            MaxCikis          = maxCikis,
            CurrencyCards     = currencyCards,
            DailyTrend        = dailyTrend,
            RecentTransactions = recent
        };

        return OperationResult<DashboardDto>.Ok(dto);
    }

    private static string DisplayCurrency(CurrencyType c) => c switch
    {
        CurrencyType.TRY => "TL",
        CurrencyType.USD => "USD",
        CurrencyType.EUR => "EUR",
        _                => c.ToString()
    };

    private static string DisplayType(TransactionType t) => t switch
    {
        TransactionType.Giris => "Giriş",
        TransactionType.Cikis => "Çıkış",
        _                     => t.ToString()
    };
}
