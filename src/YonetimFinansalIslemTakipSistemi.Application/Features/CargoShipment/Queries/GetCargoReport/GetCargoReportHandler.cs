using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoReport;

public class GetCargoReportHandler
{
    private readonly ICargoShipmentRepository _repository;
    private readonly IUserContext             _userContext;

    public GetCargoReportHandler(ICargoShipmentRepository repository, IUserContext userContext)
    {
        _repository  = repository;
        _userContext = userContext;
    }

    public async Task<OperationResult<CargoReportDto>> HandleAsync(GetCargoReportQuery query)
    {
        var hasPermission =
            _userContext.HasPermission(PermissionType.CanViewCargoModule)       ||
            _userContext.HasPermission(PermissionType.CanViewIncomingCargo)     ||
            _userContext.HasPermission(PermissionType.CanManageIncomingCargo)   ||
            _userContext.HasPermission(PermissionType.CanViewOutgoingCargo)     ||
            _userContext.HasPermission(PermissionType.CanManageOutgoingCargo);

        if (!hasPermission)
            return OperationResult<CargoReportDto>.Fail("Kargo raporu için yetkiniz bulunmamaktadır.");

        // Server-side: tarih / enum / ID filtreleme
        var rows = (await _repository.GetFilteredReportAsync(
            query.DateFrom, query.DateTo, query.Direction,
            query.CargoCompanyId, query.Status, query.NotificationStatus, query.Priority)).ToList();

        // In-memory: EF Core PostgreSQL string çevirisi yerine burada yapılıyor
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            rows = rows.Where(s =>
                (s.CompanyDirectory?.CompanyName.Contains(kw, StringComparison.OrdinalIgnoreCase) == true) ||
                (s.SenderName?.Contains(kw, StringComparison.OrdinalIgnoreCase) == true) ||
                (s.ReceiverName?.Contains(kw, StringComparison.OrdinalIgnoreCase) == true)
            ).ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.VehiclePlate))
        {
            var vp = query.VehiclePlate.Trim();
            rows = rows.Where(s => s.VehiclePlate?.Contains(vp, StringComparison.OrdinalIgnoreCase) == true).ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.TrackingNumber))
        {
            var tn = query.TrackingNumber.Trim();
            rows = rows.Where(s => s.TrackingNumber?.Contains(tn, StringComparison.OrdinalIgnoreCase) == true).ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.ShipmentNumber))
        {
            var sn = query.ShipmentNumber.Trim();
            rows = rows.Where(s => s.ShipmentNumber?.Contains(sn, StringComparison.OrdinalIgnoreCase) == true).ToList();
        }

        var rowDtos = rows.Select(MapToRow).ToList();

        var dto = new CargoReportDto
        {
            Rows           = rowDtos,
            TotalCount     = rowDtos.Count,
            IncomingCount  = rowDtos.Count(r => r.Direction == CargoShipmentDirection.Incoming),
            OutgoingCount  = rowDtos.Count(r => r.Direction == CargoShipmentDirection.Outgoing),
            PendingCount   = rows.Count(s =>
                s.Status != CargoShipmentStatus.Delivered &&
                s.Status != CargoShipmentStatus.Cancelled),
            DeliveredCount = rows.Count(s => s.Status == CargoShipmentStatus.Delivered),
            FilterSummary  = BuildFilterSummary(query),
            DateFrom       = query.DateFrom,
            DateTo         = query.DateTo,
        };

        return OperationResult<CargoReportDto>.Ok(dto);
    }

    private static CargoReportRowDto MapToRow(Domain.Entities.CargoShipment s) => new()
    {
        Id                        = s.Id,
        ShipmentNumber            = s.ShipmentNumber,
        Direction                 = s.Direction,
        DirectionDisplay          = s.Direction == CargoShipmentDirection.Incoming ? "Gelen" : "Giden",
        ShipmentDate              = s.ShipmentDate,
        Party                     = s.CompanyDirectory?.CompanyName
                                    ?? (s.Direction == CargoShipmentDirection.Incoming ? s.SenderName : s.ReceiverName),
        CargoCompanyName          = s.CargoCompany?.Name,
        TrackingNumber            = s.TrackingNumber,
        VehiclePlate              = s.VehiclePlate,
        StatusDisplay             = DisplayStatus(s.Status),
        NotificationStatusDisplay = DisplayNotificationStatus(s.NotificationStatus),
        PriorityDisplay           = DisplayPriority(s.Priority),
    };

    private static string BuildFilterSummary(GetCargoReportQuery query)
    {
        var parts = new List<string>();

        if (query.DateFrom.HasValue || query.DateTo.HasValue)
        {
            var from = query.DateFrom?.ToString("dd.MM.yyyy") ?? "—";
            var to   = query.DateTo?.ToString("dd.MM.yyyy")   ?? "—";
            parts.Add($"Tarih: {from} — {to}");
        }

        if (query.Direction.HasValue)
            parts.Add($"Yön: {(query.Direction.Value == CargoShipmentDirection.Incoming ? "Gelen" : "Giden")}");

        if (!string.IsNullOrWhiteSpace(query.CargoCompanyName))
            parts.Add($"Kargo Firması: {query.CargoCompanyName}");

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            parts.Add($"Karşı Taraf: {query.Keyword}");

        if (query.Status.HasValue)
            parts.Add($"Durum: {DisplayStatus(query.Status.Value)}");

        if (query.Priority.HasValue)
            parts.Add($"Öncelik: {DisplayPriority(query.Priority.Value)}");

        return parts.Count > 0 ? string.Join(" | ", parts) : "Tüm kayıtlar";
    }

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
