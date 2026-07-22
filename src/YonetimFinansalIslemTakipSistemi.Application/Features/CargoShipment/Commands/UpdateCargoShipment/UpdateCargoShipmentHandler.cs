using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.UpdateCargoShipment;

public class UpdateCargoShipmentHandler
{
    private readonly ICargoShipmentRepository    _repository;
    private readonly IAuditLogService            _auditLogService;
    private readonly IUserContext                _userContext;
    private readonly ICargoDashboardCacheService _cache;
    private readonly IUserTextNormalizationService _textNormalization;

    public UpdateCargoShipmentHandler(
        ICargoShipmentRepository    repository,
        IAuditLogService            auditLogService,
        IUserContext                userContext,
        ICargoDashboardCacheService cache,
        IUserTextNormalizationService textNormalization)
    {
        _repository             = repository;
        _auditLogService        = auditLogService;
        _userContext            = userContext;
        _cache                  = cache;
        _textNormalization      = textNormalization;
    }

    public async Task<OperationResult<bool>> HandleAsync(UpdateCargoShipmentRequest request)
    {
        var requiredPermission = request.Direction == CargoShipmentDirection.Incoming
            ? PermissionType.CanManageIncomingCargo
            : PermissionType.CanManageOutgoingCargo;

        if (!_userContext.HasPermission(requiredPermission))
            return OperationResult<bool>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        if (request.ShipmentDate == default)
            return OperationResult<bool>.Fail("Kargo tarihi zorunludur.");

        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity is null)
            return OperationResult<bool>.Fail("Kargo kaydı bulunamadı.");

        // Durum geçiş kontrolü — geçersiz geçişler reddedilir
        if (!CargoStatusTransitions.IsAllowed(entity.Status, request.Status))
            return OperationResult<bool>.Fail(
                $"'{DisplayStatus(entity.Status)}' durumundan '{DisplayStatus(request.Status)}' durumuna geçiş geçersizdir.");

        // Sadece değişen alanları audit'e yaz — ileride Timeline bu formatı kullanacak
        var (oldValues, newValues) = BuildAuditDiff(entity, request);

        // ShipmentNumber otomatik üretilir ve değiştirilemez — request'ten alınmaz, mevcut değer korunur
        entity.Direction          = request.Direction;
        entity.ShipmentDate       = DateTime.SpecifyKind(request.ShipmentDate.Date, DateTimeKind.Utc);
        entity.ShipmentTime       = request.ShipmentTime;
        entity.ShipmentType       = request.ShipmentType;
        entity.Priority           = request.Priority;
        entity.CargoCompanyId     = request.CargoCompanyId;
        entity.CompanyDirectoryId = request.CompanyDirectoryId;
        // Harf dönüşümü kullanıcı tercihine göre merkezi serviste yapılır
        entity.SenderName         = _textNormalization.Normalize(request.SenderName);
        entity.ReceiverName       = _textNormalization.Normalize(request.ReceiverName);
        entity.DeliveredBy        = _textNormalization.Normalize(request.DeliveredBy);
        entity.ReceivedBy         = _textNormalization.Normalize(request.ReceivedBy);
        entity.VehiclePlate       = _textNormalization.Normalize(request.VehiclePlate);
        entity.TrackingNumber     = request.TrackingNumber?.Trim();
        // TrackingUrl: eski kayıtlardaki değer korunur; yeni üretim yok —
        // portal/takip bağlantısının tek kaynağı CargoCompany.PortalUrl
        entity.Status             = request.Status;
        entity.NotificationStatus = request.NotificationStatus;
        entity.Notes              = _textNormalization.Normalize(request.Notes);
        entity.UpdatedByUserId    = request.UpdatedByUserId;
        entity.UpdatedAt          = DateTime.UtcNow;

        // Kullanıcı bilinçli olarak "Firma Bilgilerini Yenile" bastıysa snapshot güncellenir
        if (request.UpdateSnapshot)
        {
            entity.ReceiverCompanyNameSnapshot = request.SnapshotCompanyName;
            entity.ReceiverAddressSnapshot     = request.SnapshotAddress;
            // Dikkatine kullanıcı girişi — harf tercihine tabi
            entity.ReceiverAttentionSnapshot   = _textNormalization.Normalize(request.SnapshotAttention);
            entity.ReceiverCitySnapshot        = request.SnapshotCity;
            entity.ReceiverDistrictSnapshot    = request.SnapshotDistrict;
            entity.ReceiverPhoneSnapshot       = request.SnapshotPhone;
            entity.ReceiverEmailSnapshot       = request.SnapshotEmail;
        }

        await _repository.UpdateAsync(entity);

        await _auditLogService.WriteAsync(
            AuditAction.CargoShipmentUpdated,
            _userContext.UserId,
            _userContext.FullName,
            "CargoShipment", entity.Id,
            oldValues, newValues);

        // Güncelleme sonrası dashboard cache geçersiz
        _cache.Invalidate();
        return OperationResult<bool>.Ok(true);
    }

    /// <summary>
    /// Sadece değişen alanları karşılaştırır; oldValues/newValues'u Timeline-uyumlu formatta üretir.
    /// Format: "Alan: EskiDeğer | Alan2: EskiDeğer2" — ileride Timeline handler bu formatı parse eder.
    /// </summary>
    private static (string oldValues, string newValues) BuildAuditDiff(
        Domain.Entities.CargoShipment entity, UpdateCargoShipmentRequest request)
    {
        var oldParts = new List<string>();
        var newParts = new List<string>();

        if (entity.Status != request.Status)
        {
            oldParts.Add($"Durum: {DisplayStatus(entity.Status)}");
            newParts.Add($"Durum: {DisplayStatus(request.Status)}");
        }

        if (entity.NotificationStatus != request.NotificationStatus)
        {
            oldParts.Add($"Bildirim: {DisplayNotificationStatus(entity.NotificationStatus)}");
            newParts.Add($"Bildirim: {DisplayNotificationStatus(request.NotificationStatus)}");
        }

        if (entity.Priority != request.Priority)
        {
            oldParts.Add($"Öncelik: {DisplayPriority(entity.Priority)}");
            newParts.Add($"Öncelik: {DisplayPriority(request.Priority)}");
        }

        if (request.UpdateSnapshot)
            newParts.Add("Firma snapshot güncellendi");

        var old = oldParts.Count > 0 ? string.Join(" | ", oldParts) : "Kargo güncellendi";
        var @new = newParts.Count > 0 ? string.Join(" | ", newParts) : "Kargo güncellendi";
        return (old, @new);
    }

    private static string DisplayStatus(CargoShipmentStatus s) => s switch
    {
        CargoShipmentStatus.Draft              => "Taslak",             // eski kayıt etiketi — audit log uyumluluğu
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

    private static string DisplayPriority(CargoShipmentPriority p) => p switch
    {
        CargoShipmentPriority.Medium   => "Orta",
        CargoShipmentPriority.Urgent   => "Acil",
        CargoShipmentPriority.Critical => "Çok Acil",
        _                              => "Normal"
    };
}
