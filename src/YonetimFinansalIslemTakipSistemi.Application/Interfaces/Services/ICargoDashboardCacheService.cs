using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoDashboard;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Kargo dashboard sonucunu 60 saniyelik TTL ile bellekte tutar.
/// Singleton olarak kaydedilmeli.
/// </summary>
public interface ICargoDashboardCacheService
{
    /// <summary>Geçerli cache varsa döner; yoksa null.</summary>
    CargoDashboardDto? Get();
    void Set(CargoDashboardDto dto);
    void Invalidate();
}
