using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoDashboard;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// Kargo dashboard sonucunu 60 saniye bellekte tutar.
/// Singleton olarak kaydedilir; tüm oturumlar aynı önbelleği paylaşır.
/// </summary>
public class InMemoryCargoDashboardCacheService : ICargoDashboardCacheService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    private readonly object      _lock      = new();
    private CargoDashboardDto?   _cached;
    private DateTime             _expiresAt = DateTime.MinValue;

    public CargoDashboardDto? Get()
    {
        lock (_lock)
        {
            if (_cached is not null && DateTime.UtcNow < _expiresAt)
                return _cached;

            _cached = null;
            return null;
        }
    }

    public void Set(CargoDashboardDto dto)
    {
        lock (_lock)
        {
            _cached    = dto;
            _expiresAt = DateTime.UtcNow.Add(Ttl);
        }
    }

    public void Invalidate()
    {
        lock (_lock)
        {
            _cached    = null;
            _expiresAt = DateTime.MinValue;
        }
    }
}
