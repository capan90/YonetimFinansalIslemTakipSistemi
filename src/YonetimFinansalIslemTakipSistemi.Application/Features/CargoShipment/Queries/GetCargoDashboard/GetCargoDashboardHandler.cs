using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoDashboard;

public class GetCargoDashboardHandler
{
    private readonly ICargoShipmentRepository     _repository;
    private readonly IUserContext                 _userContext;
    private readonly ICargoDashboardCacheService  _cache;

    public GetCargoDashboardHandler(
        ICargoShipmentRepository    repository,
        IUserContext                userContext,
        ICargoDashboardCacheService cache)
    {
        _repository  = repository;
        _userContext = userContext;
        _cache       = cache;
    }

    public async Task<OperationResult<CargoDashboardDto>> HandleAsync(GetCargoDashboardQuery query)
    {
        // Dashboard yalnızca CanViewCargoModule yetkisi gerektiriyor
        if (!_userContext.HasPermission(PermissionType.CanViewCargoModule))
            return OperationResult<CargoDashboardDto>.Fail("Dashboard için CanViewCargoModule yetkisi gereklidir.");

        // Cache kontrolü — BypassCache=true ise ("Yenile" butonu) DB'den taze veri çek
        if (!query.BypassCache)
        {
            var cached = _cache.Get();
            if (cached is not null)
                return OperationResult<CargoDashboardDto>.Ok(cached);
        }

        var all   = await _repository.GetAllActiveAsync();
        var today = DateTime.Today;
        var from  = query.ChartDateFrom.Date;
        var to    = query.ChartDateTo.Date;

        // ── Özet Kartlar ─────────────────────────────────────────────────
        var dto = new CargoDashboardDto
        {
            TodayIncoming = all.Count(s =>
                s.ShipmentDate.Date == today &&
                s.Direction == CargoShipmentDirection.Incoming),

            TodayOutgoing = all.Count(s =>
                s.ShipmentDate.Date == today &&
                s.Direction == CargoShipmentDirection.Outgoing),

            // Teslim edilmemiş ve iptal edilmemiş
            Pending = all.Count(s =>
                s.Status != CargoShipmentStatus.Delivered &&
                s.Status != CargoShipmentStatus.Cancelled),

            // Bildirim yapılmamış + aktif kargo durumu
            NotificationPending = all.Count(s =>
                s.NotificationStatus == CargoNotificationStatus.NotNotified &&
                (s.Status == CargoShipmentStatus.Prepared ||
                 s.Status == CargoShipmentStatus.Shipped  ||
                 s.Status == CargoShipmentStatus.Received)),

            // Acil veya Çok Acil + aktif
            UrgentPending = all.Count(s =>
                (s.Priority == CargoShipmentPriority.Urgent ||
                 s.Priority == CargoShipmentPriority.Critical) &&
                s.Status != CargoShipmentStatus.Delivered &&
                s.Status != CargoShipmentStatus.Cancelled),

            // UpdatedAt bugünse kullan; yoksa ShipmentDate
            TodayDelivered = all.Count(s =>
                s.Status == CargoShipmentStatus.Delivered &&
                (s.UpdatedAt.HasValue
                    ? s.UpdatedAt.Value.Date == today
                    : s.ShipmentDate.Date == today)),
        };

        // ── Grafik 1: Gelen/Giden (seçili tarih aralığı) ────────────────
        var range = all.Where(s => s.ShipmentDate.Date >= from && s.ShipmentDate.Date <= to).ToList();
        dto.DirectionChart =
        [
            new("Gelen", range.Count(s => s.Direction == CargoShipmentDirection.Incoming), "#2E5984"),
            new("Giden", range.Count(s => s.Direction == CargoShipmentDirection.Outgoing), "#5B9BD5"),
        ];

        // ── Grafik 2: Durum Dağılımı (tüm aktif kayıtlar) ──────────────
        dto.StatusChart =
        [
            new("Taslak",        all.Count(s => s.Status == CargoShipmentStatus.Draft),     "#64748B"),
            new("Hazırlandı",    all.Count(s => s.Status == CargoShipmentStatus.Prepared),  "#3B82F6"),
            new("Gönderildi",    all.Count(s => s.Status == CargoShipmentStatus.Shipped),   "#F59E0B"),
            new("Alındı",        all.Count(s => s.Status == CargoShipmentStatus.Received),  "#8B5CF6"),
            new("Teslim Edildi", all.Count(s => s.Status == CargoShipmentStatus.Delivered), "#10B981"),
            new("İptal",         all.Count(s => s.Status == CargoShipmentStatus.Cancelled), "#EF4444"),
        ];

        // ── Grafik 3: Top 5 Kargo Firması ───────────────────────────────
        string[] companyColors = ["#2E5984", "#4A7FB5", "#6699CC", "#3B6998", "#5A8AB0"];
        dto.CompanyChart = all
            .Where(s => !string.IsNullOrEmpty(s.CargoCompany?.Name))
            .GroupBy(s => s.CargoCompany!.Name)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select((g, i) => new CargoDashboardChartItem(g.Key, g.Count(), companyColors[i]))
            .ToList();

        // ── Son 10 Hareket ───────────────────────────────────────────────
        dto.RecentShipments = all
            .OrderByDescending(s => s.CreatedAt)
            .Take(10)
            .Select(MapToRecent)
            .ToList();

        // Sonucu önbelleğe al
        _cache.Set(dto);
        return OperationResult<CargoDashboardDto>.Ok(dto);
    }

    private static CargoDashboardRecentDto MapToRecent(Domain.Entities.CargoShipment s) => new()
    {
        Id                        = s.Id,
        ShipmentNumber            = s.ShipmentNumber,
        DirectionDisplay          = s.Direction == CargoShipmentDirection.Incoming ? "Gelen" : "Giden",
        ShipmentDate              = s.ShipmentDate,
        Party                     = s.CompanyDirectory?.CompanyName
                                    ?? (s.Direction == CargoShipmentDirection.Incoming ? s.SenderName : s.ReceiverName),
        CargoCompanyName          = s.CargoCompany?.Name,
        StatusDisplay             = DisplayStatus(s.Status),
        NotificationStatusDisplay = DisplayNotificationStatus(s.NotificationStatus),
        PriorityDisplay           = DisplayPriority(s.Priority),
    };

    private static string DisplayStatus(CargoShipmentStatus s) => s switch
    {
        CargoShipmentStatus.Draft     => "Taslak",
        CargoShipmentStatus.Prepared  => "Hazırlandı",
        CargoShipmentStatus.Shipped   => "Gönderildi",
        CargoShipmentStatus.Received  => "Alındı",
        CargoShipmentStatus.Delivered => "Teslim Edildi",
        CargoShipmentStatus.Cancelled => "İptal",
        _                             => s.ToString()
    };

    private static string DisplayNotificationStatus(CargoNotificationStatus ns) => ns switch
    {
        CargoNotificationStatus.NotNotified      => "Bildirilmedi",
        CargoNotificationStatus.WhatsAppPrepared => "WhatsApp Hazır",
        CargoNotificationStatus.MailPrepared     => "Mail Hazır",
        CargoNotificationStatus.Notified         => "Bildirildi",
        _                                        => ns.ToString()
    };

    private static string DisplayPriority(CargoShipmentPriority p) => p switch
    {
        CargoShipmentPriority.Normal   => "Normal",
        CargoShipmentPriority.Medium   => "Orta",
        CargoShipmentPriority.Urgent   => "Acil",
        CargoShipmentPriority.Critical => "Çok Acil",
        _                              => p.ToString()
    };
}
