using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;

/// <summary>
/// İzin verilen kargo durum geçişlerini tanımlar.
/// Gelen kargoda Prepared/Shipped anlamlı değildir; bu yüzden direction-aware overload kullanılır.
/// </summary>
public static class CargoStatusTransitions
{
    // Giden kargo: tüm durumlar anlamlıdır
    private static readonly Dictionary<CargoShipmentStatus, CargoShipmentStatus[]> _allowed = new()
    {
        [CargoShipmentStatus.Draft]     = [CargoShipmentStatus.Prepared,  CargoShipmentStatus.Cancelled],
        [CargoShipmentStatus.Prepared]  = [CargoShipmentStatus.Shipped,   CargoShipmentStatus.Received, CargoShipmentStatus.Cancelled],
        [CargoShipmentStatus.Shipped]   = [CargoShipmentStatus.Delivered, CargoShipmentStatus.Cancelled],
        [CargoShipmentStatus.Received]  = [CargoShipmentStatus.Delivered, CargoShipmentStatus.Cancelled],
        [CargoShipmentStatus.Delivered] = [],
        [CargoShipmentStatus.Cancelled] = [],
    };

    // Gelen kargo: Hazırlandı ve Gönderildi aşamaları atlanır; Draft'tan Alındı'ya doğrudan geçilir
    private static readonly Dictionary<CargoShipmentStatus, CargoShipmentStatus[]> _allowedIncoming = new()
    {
        [CargoShipmentStatus.Draft]     = [CargoShipmentStatus.Received, CargoShipmentStatus.Cancelled],
        [CargoShipmentStatus.Prepared]  = [CargoShipmentStatus.Received, CargoShipmentStatus.Cancelled], // eski kayıtlarda olabilir
        [CargoShipmentStatus.Shipped]   = [CargoShipmentStatus.Received, CargoShipmentStatus.Cancelled], // eski kayıtlarda olabilir
        [CargoShipmentStatus.Received]  = [CargoShipmentStatus.Delivered, CargoShipmentStatus.Cancelled],
        [CargoShipmentStatus.Delivered] = [],
        [CargoShipmentStatus.Cancelled] = [],
    };

    /// <summary>Aynı duruma kalmak her zaman geçerlidir.</summary>
    public static bool IsAllowed(CargoShipmentStatus from, CargoShipmentStatus to)
    {
        if (from == to) return true;
        return _allowed.TryGetValue(from, out var allowed) && Array.IndexOf(allowed, to) >= 0;
    }

    /// <summary>Mevcut durumdan geçilebilecek durumlar (mevcut durum dahil). Yön bilgisi gerektirmez.</summary>
    public static CargoShipmentStatus[] GetAllowedNext(CargoShipmentStatus current)
        => BuildAllowed(_allowed, current);

    /// <summary>
    /// Yön bazlı geçiş listesi. Gelen kargoda Hazırlandı ve Gönderildi gösterilmez.
    /// </summary>
    public static CargoShipmentStatus[] GetAllowedNext(CargoShipmentStatus current, CargoShipmentDirection direction)
    {
        var map = direction == CargoShipmentDirection.Incoming ? _allowedIncoming : _allowed;
        return BuildAllowed(map, current);
    }

    private static CargoShipmentStatus[] BuildAllowed(
        Dictionary<CargoShipmentStatus, CargoShipmentStatus[]> map,
        CargoShipmentStatus current)
    {
        if (!map.TryGetValue(current, out var next) || next.Length == 0)
            return [current];

        var result = new CargoShipmentStatus[next.Length + 1];
        result[0] = current;
        Array.Copy(next, 0, result, 1, next.Length);
        return result;
    }
}
