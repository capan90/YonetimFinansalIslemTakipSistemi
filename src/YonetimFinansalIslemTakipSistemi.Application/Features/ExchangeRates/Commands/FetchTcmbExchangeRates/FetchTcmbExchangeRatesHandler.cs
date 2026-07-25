using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.ExchangeRates.Commands.CreateOrUpdateExchangeRate;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.ExchangeRates.Commands.FetchTcmbExchangeRates;

/// <summary>
/// TCMB'den USD/EUR kurlarını çeker ve mevcut upsert akışıyla kaydeder.
/// Kaydetme, yetki kontrolü ve audit için CreateOrUpdateExchangeRateHandler yeniden kullanılır
/// (tek doğrulama/audit noktası — kural tekrarı yok).
/// </summary>
public class FetchTcmbExchangeRatesHandler
{
    private readonly IExchangeRateSource               _source;
    private readonly CreateOrUpdateExchangeRateHandler _upsertHandler;
    private readonly IUserContext                      _userContext;

    public FetchTcmbExchangeRatesHandler(
        IExchangeRateSource               source,
        CreateOrUpdateExchangeRateHandler upsertHandler,
        IUserContext                      userContext)
    {
        _source        = source;
        _upsertHandler = upsertHandler;
        _userContext   = userContext;
    }

    public async Task<OperationResult<FetchTcmbExchangeRatesResult>> HandleAsync(DateTime rateDate)
    {
        // Erken yetki kontrolü (upsert de kontrol eder; burada net mesaj + gereksiz ağ çağrısı yapılmaz)
        if (!_userContext.HasPermission(PermissionType.CanManageExchangeRates))
            return OperationResult<FetchTcmbExchangeRatesResult>.Fail(
                "Bu işlem için yetkiniz bulunmamaktadır.");

        if (rateDate.Date > DateTime.Today)
            return OperationResult<FetchTcmbExchangeRatesResult>.Fail(
                "Gelecek tarih için kur çekilemez.");

        IReadOnlyList<ExchangeRateSourceData> rates;
        try
        {
            rates = await _source.FetchAsync(rateDate.Date);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Ağ/timeout — kullanıcıya anlaşılır mesaj (TCMB erişilemedi)
            return OperationResult<FetchTcmbExchangeRatesResult>.Fail(
                "TCMB kur servisine ulaşılamadı. İnternet bağlantınızı kontrol edip tekrar deneyin.");
        }

        if (rates.Count == 0)
            return OperationResult<FetchTcmbExchangeRatesResult>.Fail(
                "TCMB'den kur bilgisi alınamadı. Seçilen tarih için (hafta sonu/resmi tatil) yayın olmayabilir.");

        // Her para birimi mevcut upsert akışından geçer (doğrulama + audit orada)
        var saved = 0;
        foreach (var rate in rates)
        {
            var result = await _upsertHandler.HandleAsync(new CreateOrUpdateExchangeRateCommand
            {
                RateDate     = rateDate.Date,
                CurrencyType = rate.CurrencyType,
                ForexBuying  = rate.ForexBuying,
                ForexSelling = rate.ForexSelling
            });

            if (result.Success) saved++;
        }

        if (saved == 0)
            return OperationResult<FetchTcmbExchangeRatesResult>.Fail(
                "Kurlar alındı ancak kaydedilemedi.");

        return OperationResult<FetchTcmbExchangeRatesResult>.Ok(
            new FetchTcmbExchangeRatesResult(saved, rateDate.Date));
    }
}
