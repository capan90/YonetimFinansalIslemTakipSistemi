using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;

public class GetCargoShipmentListHandler
{
    private readonly ICargoShipmentRepository _repository;
    private readonly IUserContext _userContext;

    public GetCargoShipmentListHandler(
        ICargoShipmentRepository repository,
        IUserContext userContext)
    {
        _repository  = repository;
        _userContext = userContext;
    }

    public async Task<List<CargoShipmentDto>> HandleAsync(GetCargoShipmentListQuery query)
    {
        // Yetki: CanView veya CanManage — Manage izni View'u da kapsar
        var viewPermission   = query.Direction == CargoShipmentDirection.Incoming
            ? PermissionType.CanViewIncomingCargo
            : PermissionType.CanViewOutgoingCargo;
        var managePermission = query.Direction == CargoShipmentDirection.Incoming
            ? PermissionType.CanManageIncomingCargo
            : PermissionType.CanManageOutgoingCargo;

        if (!_userContext.HasPermission(viewPermission) && !_userContext.HasPermission(managePermission))
            return [];

        // Tarih/durum/öncelik filtreleri SQL'de uygulanır (tüm yön verisi belleğe çekilmez);
        // GetFilteredReportAsync rapor ile aynı sorguyu paylaşır — ayrı sorgu kodu yok.
        // Serbest metin araması Türkçe OrdinalIgnoreCase semantiği için bellekte kalır.
        IEnumerable<Domain.Entities.CargoShipment> filtered =
            await _repository.GetFilteredReportAsync(
                query.DateFrom, query.DateTo, query.Direction,
                cargoCompanyId: null, status: query.Status,
                notificationStatus: null, priority: query.Priority);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            filtered = query.SearchType switch
            {
                "Firma"        => filtered.Where(x =>
                    (x.CargoCompany     != null && x.CargoCompany.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                    (x.CompanyDirectory != null && x.CompanyDirectory.CompanyName.Contains(kw, StringComparison.OrdinalIgnoreCase))),
                "Kargo No"     => filtered.Where(x =>
                    x.ShipmentNumber != null && x.ShipmentNumber.Contains(kw, StringComparison.OrdinalIgnoreCase)),
                "Takip No"     => filtered.Where(x =>
                    x.TrackingNumber != null && x.TrackingNumber.Contains(kw, StringComparison.OrdinalIgnoreCase)),
                "Araç Plakası" => filtered.Where(x =>
                    x.VehiclePlate != null && x.VehiclePlate.Contains(kw, StringComparison.OrdinalIgnoreCase)),
                // null veya "Genel" — tüm alanlarda arama
                _ => filtered.Where(x =>
                    (x.ShipmentNumber   != null && x.ShipmentNumber.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                    (x.TrackingNumber   != null && x.TrackingNumber.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                    (x.VehiclePlate     != null && x.VehiclePlate.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                    (x.SenderName       != null && x.SenderName.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                    (x.ReceiverName     != null && x.ReceiverName.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                    (x.CargoCompany     != null && x.CargoCompany.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)) ||
                    (x.CompanyDirectory != null && x.CompanyDirectory.CompanyName.Contains(kw, StringComparison.OrdinalIgnoreCase)))
            };
        }

        return filtered
            .OrderByDescending(x => x.ShipmentDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new CargoShipmentDto
            {
                Id                   = x.Id,
                ShipmentNumber       = x.ShipmentNumber,
                Direction            = x.Direction,
                ShipmentDate         = x.ShipmentDate,
                ShipmentTime         = x.ShipmentTime,
                ShipmentType         = x.ShipmentType,
                ShipmentTypeDisplay  = DisplayShipmentType(x.ShipmentType),
                Priority             = x.Priority,
                PriorityDisplay      = DisplayPriority(x.Priority),
                CargoCompanyId       = x.CargoCompanyId,
                CargoCompanyName     = x.CargoCompany?.Name,
                CompanyDirectoryId   = x.CompanyDirectoryId,
                CompanyDirectoryName = x.CompanyDirectory?.CompanyName,
                SenderName           = x.SenderName,
                ReceiverName         = x.ReceiverName,
                DeliveredBy          = x.DeliveredBy,
                ReceivedBy           = x.ReceivedBy,
                VehiclePlate         = x.VehiclePlate,
                TrackingNumber       = x.TrackingNumber,
                // Tek bağlantı kaynağı: firma PortalUrl; eski kayıtlarda saklı TrackingUrl korunur
                TrackingUrl          = !string.IsNullOrWhiteSpace(x.TrackingUrl)
                                        ? x.TrackingUrl
                                        : x.CargoCompany?.PortalUrl,
                Status                    = x.Status,
                StatusDisplay             = DisplayStatus(x.Status, x.Direction),
                NotificationStatus        = x.NotificationStatus,
                NotificationStatusDisplay = DisplayNotificationStatus(x.NotificationStatus),
                DisplayParty                  = BuildDisplayParty(x),
                Notes                         = x.Notes,
                ReceiverAttentionSnapshot     = x.ReceiverAttentionSnapshot,
                CreatedAt                     = x.CreatedAt
            })
            .ToList();
    }

    private static string DisplayStatus(CargoShipmentStatus s, CargoShipmentDirection d) => s switch
    {
        CargoShipmentStatus.Draft              => d == CargoShipmentDirection.Incoming ? "Bekleniyor" : "Gönderime Hazır",
        CargoShipmentStatus.Prepared           => "Gönderime Hazır",
        CargoShipmentStatus.HandedToCargo      => "Kargoya Teslim Edildi",
        CargoShipmentStatus.Shipped            => "Gönderildi",
        CargoShipmentStatus.Waiting            => "Bekleniyor",
        CargoShipmentStatus.Received           => "Teslim Alındı",
        CargoShipmentStatus.PersonnelDelivered => "Personele Teslim Edildi",
        CargoShipmentStatus.Delivered          => "Teslim Edildi",
        CargoShipmentStatus.Cancelled          => "İptal",
        _                                      => s.ToString()
    };

    private static string DisplayNotificationStatus(CargoNotificationStatus ns) => ns switch
    {
        CargoNotificationStatus.NotNotified      => "Bildirilmedi",
        CargoNotificationStatus.WhatsAppPrepared => "WhatsApp Hazır",
        CargoNotificationStatus.MailPrepared     => "Mail Hazır",
        CargoNotificationStatus.Notified         => "Bildirildi",
        _                                        => ns.ToString()
    };

    private static string BuildDisplayParty(Domain.Entities.CargoShipment x)
    {
        if (!string.IsNullOrWhiteSpace(x.CompanyDirectory?.CompanyName))
            return x.CompanyDirectory.CompanyName;

        var parts = new[] { x.SenderName, x.ReceiverName }
            .Where(s => !string.IsNullOrWhiteSpace(s));
        return string.Join(" / ", parts);
    }

    private static string DisplayPriority(CargoShipmentPriority p) => p switch
    {
        CargoShipmentPriority.Medium   => "Orta",
        CargoShipmentPriority.Urgent   => "Acil",
        CargoShipmentPriority.Critical => "Çok Acil",
        _                              => "Normal"
    };

    private static string? DisplayShipmentType(CargoShipmentType? t) => t switch
    {
        CargoShipmentType.Document  => "Evrak",
        CargoShipmentType.Sample    => "Numune",
        CargoShipmentType.Invoice   => "Fatura",
        CargoShipmentType.Contract  => "Sözleşme",
        CargoShipmentType.SparePart => "Yedek Parça",
        CargoShipmentType.Other     => "Diğer",
        null                        => null,
        _                           => t.ToString()
    };
}
