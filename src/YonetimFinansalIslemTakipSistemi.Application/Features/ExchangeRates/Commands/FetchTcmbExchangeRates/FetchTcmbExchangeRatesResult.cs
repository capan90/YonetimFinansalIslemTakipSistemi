namespace YonetimFinansalIslemTakipSistemi.Application.Features.ExchangeRates.Commands.FetchTcmbExchangeRates;

/// <summary>TCMB'den çekim sonucu özeti — UI kullanıcıya kaç kur kaydedildiğini bildirir.</summary>
public sealed record FetchTcmbExchangeRatesResult(int SavedCount, DateTime RateDate);
