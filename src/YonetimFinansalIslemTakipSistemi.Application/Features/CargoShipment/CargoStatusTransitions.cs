using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;

/// <summary>
/// İzin verilen kargo durum geçişlerini tanımlar.
/// Gelen kargoda Received, giden kargoda Shipped anlamlıdır;
/// ancak geçiş kuralları her iki yön için aynıdır.
/// </summary>
public static class CargoStatusTransitions
{
    private static readonly Dictionary<CargoShipmentStatus, CargoShipmentStatus[]> _allowed = new()
    {
        [CargoShipmentStatus.Draft]     = [CargoShipmentStatus.Prepared,  CargoShipmentStatus.Cancelled],
        [CargoShipmentStatus.Prepared]  = [CargoShipmentStatus.Shipped,   CargoShipmentStatus.Received, CargoShipmentStatus.Cancelled],
        [CargoShipmentStatus.Shipped]   = [CargoShipmentStatus.Delivered, CargoShipmentStatus.Cancelled],
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

    /// <summary>Mevcut durumdan geçilebilecek durumlar (mevcut durum dahil).</summary>
    public static CargoShipmentStatus[] GetAllowedNext(CargoShipmentStatus current)
    {
        if (!_allowed.TryGetValue(current, out var next) || next.Length == 0)
            return [current];

        var result = new CargoShipmentStatus[next.Length + 1];
        result[0] = current;
        Array.Copy(next, 0, result, 1, next.Length);
        return result;
    }
}
