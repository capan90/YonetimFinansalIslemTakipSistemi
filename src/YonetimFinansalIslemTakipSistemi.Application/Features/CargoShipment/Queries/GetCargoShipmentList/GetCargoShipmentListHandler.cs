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

        var all = await _repository.GetByDirectionAsync(query.Direction);

        IEnumerable<Domain.Entities.CargoShipment> filtered = all;

        if (query.DateFrom.HasValue)
            filtered = filtered.Where(x => x.ShipmentDate >= query.DateFrom.Value.Date);
        if (query.DateTo.HasValue)
            filtered = filtered.Where(x => x.ShipmentDate <= query.DateTo.Value.Date);
        if (query.Status.HasValue)
            filtered = filtered.Where(x => x.Status == query.Status.Value);
        if (query.Priority.HasValue)
            filtered = filtered.Where(x => x.Priority == query.Priority.Value);

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
                TrackingUrl          = x.TrackingUrl,
                Status                    = x.Status,
                StatusDisplay             = DisplayStatus(x.Status),
                NotificationStatus        = x.NotificationStatus,
                NotificationStatusDisplay = DisplayNotificationStatus(x.NotificationStatus),
                DisplayParty              = BuildDisplayParty(x),
                Notes                     = x.Notes,
                CreatedAt                 = x.CreatedAt
            })
            .ToList();
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
