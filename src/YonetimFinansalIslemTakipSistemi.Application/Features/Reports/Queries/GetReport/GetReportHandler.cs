using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
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

        var description = string.IsNullOrWhiteSpace(query.DescriptionContains)
            ? null
            : query.DescriptionContains.Trim();

        // 4. Repository aggregate sorgusu — filtreler dahil (GROUP BY PostgreSQL'de çalışır)
        var rawData = await _repository.GetReportDataAsync(
            startUtc, endExclusiveUtc,
            query.TransactionType,
            query.CurrencyType,
            description);

        // 5. Detay satırları — yalnızca istendiğinde sorgulanır
        List<TransactionDetailDto>? details = null;
        if (query.ShowTransactionDetails)
        {
            var rows = await _repository.GetFilteredForReportDetailAsync(
                startUtc, endExclusiveUtc,
                query.TransactionType,
                query.CurrencyType,
                description);

            details = BuildDetails(rows);
        }

        // 6. Application katmanında FinancialDirection iş kuralına göre DTO montajı
        var dto = BuildDto(rawData, details, query);
        return OperationResult<ReportDto>.Ok(dto);
    }

    private static ReportDto BuildDto(
        List<CurrencyReportData>    raw,
        List<TransactionDetailDto>? details,
        GetReportQuery              query)
    {
        // Filtre koşullarına göre hangi para birimleri ve türler dahil edilecek
        var currencies = query.CurrencyType.HasValue
            ? new[] { query.CurrencyType.Value }
            : Enum.GetValues<CurrencyType>();

        var types = query.TransactionType.HasValue
            ? new[] { query.TransactionType.Value }
            : Enum.GetValues<TransactionType>();

        // Para birimi bazlı özet — veri olmayan para birimi sıfırlarla tamamlanır
        var currencySummaries = currencies.Select(currency =>
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

        // İşlem türü bazlı özet — seçilen para birimleri için ayrıştırılmış tutarlar
        var typeSummaries = types.Select(type =>
        {
            var amountsByCurrency = currencies.Select(currency =>
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
            StartDate                = query.StartDate,
            EndDate                  = query.EndDate,
            FilterTransactionType    = query.TransactionType,
            FilterCurrencyType       = query.CurrencyType,
            FilterDescription        = query.DescriptionContains,
            CurrencySummaries        = currencySummaries,
            TransactionTypeSummaries = typeSummaries,
            TransactionDetails       = details
        };
    }

    private static List<TransactionDetailDto> BuildDetails(IReadOnlyList<CashTransaction> transactions)
    {
        // Para birimi bazlı kümülatif bakiye — rapor döneminin başından itibaren
        var runningBalance = new Dictionary<CurrencyType, decimal>
        {
            [CurrencyType.TRY] = 0m,
            [CurrencyType.USD] = 0m,
            [CurrencyType.EUR] = 0m
        };

        var details = new List<TransactionDetailDto>(transactions.Count);
        foreach (var t in transactions)
        {
            var isInflow = t.TransactionType.GetFinancialDirection() == FinancialDirection.Inflow;
            var borc     = isInflow ? 0m    : t.Amount;
            var alacak   = isInflow ? t.Amount : 0m;

            runningBalance[t.CurrencyType] += isInflow ? t.Amount : -t.Amount;

            details.Add(new TransactionDetailDto
            {
                TransactionDate = t.TransactionDate,
                Description     = t.Description ?? string.Empty,
                TypeDisplay     = DisplayType(t.TransactionType),
                CurrencyDisplay = DisplayCurrency(t.CurrencyType),
                Borc            = borc,
                Alacak          = alacak,
                Balance         = runningBalance[t.CurrencyType]
            });
        }
        return details;
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
