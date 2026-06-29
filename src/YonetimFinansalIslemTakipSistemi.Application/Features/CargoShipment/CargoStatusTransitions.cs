using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;

/// <summary>
/// İzin verilen kargo durum geçişlerini tanımlar.
/// Gelen ve giden kargo için ayrı kurallar uygulanır.
/// </summary>
public static class CargoStatusTransitions
{
    // Giden kargo geçiş kuralları
    private static readonly Dictionary<CargoShipmentStatus, CargoShipmentStatus[]> _allowed = new()
    {
        [CargoShipmentStatus.Draft]              = [CargoShipmentStatus.Prepared,        CargoShipmentStatus.HandedToCargo, CargoShipmentStatus.Cancelled], // eski kayıt uyumluluğu
        [CargoShipmentStatus.Prepared]           = [CargoShipmentStatus.HandedToCargo,   CargoShipmentStatus.Shipped,       CargoShipmentStatus.Delivered,        CargoShipmentStatus.Cancelled],
        [CargoShipmentStatus.HandedToCargo]      = [CargoShipmentStatus.Shipped,         CargoShipmentStatus.Delivered,     CargoShipmentStatus.Cancelled],
        [CargoShipmentStatus.Shipped]            = [CargoShipmentStatus.Delivered,       CargoShipmentStatus.Cancelled],
        [CargoShipmentStatus.Received]           = [CargoShipmentStatus.Delivered,       CargoShipmentStatus.Cancelled], // eski kayıt uyumluluğu
        [CargoShipmentStatus.Delivered]          = [],
        [CargoShipmentStatus.Cancelled]          = [],
        [CargoShipmentStatus.Waiting]            = [],
        [CargoShipmentStatus.PersonnelDelivered] = [],
    };

    // Gelen kargo geçiş kuralları
    private static readonly Dictionary<CargoShipmentStatus, CargoShipmentStatus[]> _allowedIncoming = new()
    {
        [CargoShipmentStatus.Draft]              = [CargoShipmentStatus.Waiting, CargoShipmentStatus.Received,           CargoShipmentStatus.Cancelled], // eski kayıt uyumluluğu
        [CargoShipmentStatus.Prepared]           = [CargoShipmentStatus.Waiting, CargoShipmentStatus.Received,           CargoShipmentStatus.Cancelled], // eski kayıt uyumluluğu
        [CargoShipmentStatus.Shipped]            = [CargoShipmentStatus.Waiting, CargoShipmentStatus.Received,           CargoShipmentStatus.Cancelled], // eski kayıt uyumluluğu
        [CargoShipmentStatus.Waiting]            = [CargoShipmentStatus.Received, CargoShipmentStatus.Cancelled],
        [CargoShipmentStatus.Received]           = [CargoShipmentStatus.PersonnelDelivered, CargoShipmentStatus.Cancelled],
        [CargoShipmentStatus.PersonnelDelivered] = [],
        [CargoShipmentStatus.Delivered]          = [], // eski gelen kayıtlar
        [CargoShipmentStatus.Cancelled]          = [],
        [CargoShipmentStatus.HandedToCargo]      = [],
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
    /// Yön bazlı geçiş listesi.
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
