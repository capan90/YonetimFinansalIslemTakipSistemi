using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Domain.Extensions;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Reports.Queries.GetReport;

public class GetReportHandler
{
    private readonly ICashTransactionRepository _repository;
    private readonly IUserContext               _userContext;

    public GetReportHandler(ICashTransactionRepository repository, IUserContext userContext)
    {
        _repository  = repository;
        _userContext = userContext;
    }

    public async Task<OperationResult<ReportDto>> HandleAsync(GetReportQuery query)
    {
        // 1. Yetki kontrolü — handler seviyesinde zorunlu; UI gizlemesi yalnızca UX
        if (!_userContext.HasPermission(PermissionType.CanViewReports))
            return OperationResult<ReportDto>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        // 2. Tarih validasyonu
        if (query.StartDate.HasValue && query.StartDate.Value.Date > DateTime.Today)
            return OperationResult<ReportDto>.Fail("Başlangıç tarihi bugünden ileri olamaz.");

        if (query.StartDate.HasValue && query.EndDate.HasValue
            && query.StartDate.Value.Date > query.EndDate.Value.Date)
            return OperationResult<ReportDto>.Fail("Başlangıç tarihi bitiş tarihinden büyük olamaz.");

        // 3. UTC yarı-açık aralık sınırları
        // TransactionDate her zaman gece 00:00 UTC olarak saklanır (CreateCashTransactionHandler kuralı).
        // Yarı-açık aralık: >= start, < endExclusive — bitiş günündeki kayıtlar dahil olur.
        DateTime? startUtc = query.StartDate.HasValue
            ? DateTime.SpecifyKind(query.StartDate.Value.Date, DateTimeKind.Utc)
            : null;

        DateTime? endExclusiveUtc = query.EndDate.HasValue
            ? DateTime.SpecifyKind(query.EndDate.Value.Date.AddDays(1), DateTimeKind.Utc)
            : null;

        // 4. Repository aggregate sorgusu (GROUP BY PostgreSQL'de çalışır; kayıtlar belleğe alınmaz)
        var rawData = await _repository.GetReportDataAsync(startUtc, endExclusiveUtc);

        // 5. Application katmanında FinancialDirection iş kuralına göre DTO montajı
        var dto = BuildDto(rawData, query.StartDate, query.EndDate);
        return OperationResult<ReportDto>.Ok(dto);
    }

    private static ReportDto BuildDto(
        List<CurrencyReportData> raw,
        DateTime?                startDate,
        DateTime?                endDate)
    {
        var allCurrencies = Enum.GetValues<CurrencyType>();
        var allTypes      = Enum.GetValues<TransactionType>();

        // Para birimi bazlı özet — veri olmayan para birimi sıfırlarla tamamlanır
        var currencySummaries = allCurrencies.Select(currency =>
        {
            var rows    = raw.Where(r => r.Currency == currency).ToList();
            var inflow  = rows
                .Where(r => r.TransactionType.GetFinancialDirection() == FinancialDirection.Inflow)
                .Sum(r => r.TotalAmount);
            var outflow = rows
                .Where(r => r.TransactionType.GetFinancialDirection() == FinancialDirection.Outflow)
                .Sum(r => r.TotalAmount);

            return new CurrencySummaryDto
            {
                Currency         = currency,
                CurrencyDisplay  = DisplayCurrency(currency),
                TotalInflow      = inflow,
                TotalOutflow     = outflow,
                TransactionCount = rows.Sum(r => r.Count)
            };
        }).ToList();

        // İşlem türü bazlı özet — her tür için para birimi bazlı ayrıştırılmış tutarlar
        var typeSummaries = allTypes.Select(type =>
        {
            var amountsByCurrency = allCurrencies.Select(currency =>
            {
                var row = raw.FirstOrDefault(r => r.TransactionType == type && r.Currency == currency);
                return new CurrencyAmountDto
                {
                    Currency        = currency,
                    CurrencyDisplay = DisplayCurrency(currency),
                    TotalAmount     = row?.TotalAmount ?? 0m,
                    Count           = row?.Count ?? 0
                };
            }).ToList();

            return new TransactionTypeSummaryDto
            {
                TransactionType   = type,
                TypeDisplay       = DisplayType(type),
                AmountsByCurrency = amountsByCurrency
            };
        }).ToList();

        return new ReportDto
        {
            StartDate                = startDate,
            EndDate                  = endDate,
            CurrencySummaries        = currencySummaries,
            TransactionTypeSummaries = typeSummaries
        };
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
        TransactionType.Tahsilat    => "Tahsilat",
        TransactionType.Odeme       => "Ödeme",
        TransactionType.Avans       => "Avans",
        TransactionType.OzelHarcama => "Özel Harcama",
        TransactionType.Transfer    => "Transfer",
        _                           => t.ToString()
    };
}
