using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.ExchangeRates.Queries.GetExchangeRates;

public class GetExchangeRatesHandler
{
    private readonly IExchangeRateRepository _repository;
    private readonly IUserContext            _userContext;

    public GetExchangeRatesHandler(IExchangeRateRepository repository, IUserContext userContext)
    {
        _repository  = repository;
        _userContext = userContext;
    }

    public async Task<OperationResult<List<ExchangeRateDto>>> HandleAsync(GetExchangeRatesQuery query)
    {
        if (!_userContext.HasPermission(PermissionType.CanManageExchangeRates))
            return OperationResult<List<ExchangeRateDto>>.Fail(
                "Bu işlem için yetkiniz bulunmamaktadır.");

        // Yarı-açık aralık: >= fromUtc, < toExclusiveUtc
        // DateTo günündeki kayıtları dahil etmek için bir sonraki güne taşı
        DateTime? fromUtc        = query.DateFrom.HasValue
            ? DateTime.SpecifyKind(query.DateFrom.Value.Date, DateTimeKind.Utc)
            : null;
        DateTime? toExclusiveUtc = query.DateTo.HasValue
            ? DateTime.SpecifyKind(query.DateTo.Value.Date.AddDays(1), DateTimeKind.Utc)
            : null;

        var rates = await _repository.GetFilteredAsync(fromUtc, toExclusiveUtc, query.CurrencyType);

        var dtos = rates.Select(e => new ExchangeRateDto
        {
            Id              = e.Id,
            RateDate        = e.RateDate,
            CurrencyType    = e.CurrencyType,
            CurrencyDisplay = e.CurrencyType == CurrencyType.USD ? "USD" : "EUR",
            ForexBuying     = e.ForexBuying,
            ForexSelling    = e.ForexSelling,
            CreatedAt       = e.CreatedAt,
            UpdatedAt       = e.UpdatedAt
        }).ToList();

        return OperationResult<List<ExchangeRateDto>>.Ok(dtos);
    }
}
