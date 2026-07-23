using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

/// <summary>
/// Onaylanmış içe aktarma satırlarını TEK transaction'da kaydeder (ya hep ya hiç).
/// Önizleme ile onay arasında veri değişmiş olabileceğinden kritik kontroller
/// (yetki, firma varlığı, takip no çakışması) burada YENİDEN yapılır — UI'a güvenilmez.
/// </summary>
public class ImportCargoShipmentsHandler
{
    private readonly ICargoShipmentRepository      _repository;
    private readonly ICompanyDirectoryRepository   _directoryRepository;
    private readonly ICargoCompanyRepository       _cargoCompanyRepository;
    private readonly IAuditLogService              _auditLogService;
    private readonly ISystemLogService             _systemLog;
    private readonly ICargoDashboardCacheService   _cache;
    private readonly IUserContext                  _userContext;
    private readonly IUserTextNormalizationService _textNormalization;

    public ImportCargoShipmentsHandler(
        ICargoShipmentRepository      repository,
        ICompanyDirectoryRepository   directoryRepository,
        ICargoCompanyRepository       cargoCompanyRepository,
        IAuditLogService              auditLogService,
        ISystemLogService             systemLog,
        ICargoDashboardCacheService   cache,
        IUserContext                  userContext,
        IUserTextNormalizationService textNormalization)
    {
        _repository             = repository;
        _directoryRepository    = directoryRepository;
        _cargoCompanyRepository = cargoCompanyRepository;
        _auditLogService        = auditLogService;
        _systemLog              = systemLog;
        _cache                  = cache;
        _userContext            = userContext;
        _textNormalization      = textNormalization;
    }

    public async Task<OperationResult<ImportResult>> HandleAsync(ImportCargoShipmentsRequest request)
    {
        var startedAt = DateTime.UtcNow;

        var requiredPermission = request.Direction == CargoShipmentDirection.Incoming
            ? PermissionType.CanManageIncomingCargo
            : PermissionType.CanManageOutgoingCargo;

        if (!_userContext.HasPermission(requiredPermission))
            return OperationResult<ImportResult>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        if (request.Rows.Count == 0)
            return OperationResult<ImportResult>.Fail("İçe aktarılacak satır seçilmedi.");

        if (request.CreatedByUserId == Guid.Empty)
            return OperationResult<ImportResult>.Fail("İşlemi yapan kullanıcı belirtilmelidir.");

        // Error / kesin mükerrer satırlar UI'dan sızmış olsa bile burada reddedilir
        if (request.Rows.Any(r => !r.CanInclude))
            return OperationResult<ImportResult>.Fail(
                "Hatalı veya kesin mükerrer satırlar içe aktarılamaz. Önizlemeyi yenileyin.");

        // ── Yeniden doğrulama: önizleme ile onay arasında veri değişmiş olabilir ──

        // Firma Id'leri hâlâ mevcut ve aktif mi?
        var directoryIds = (await _directoryRepository.GetAllAsync())
            .Where(d => d.IsActive).Select(d => d.Id).ToHashSet();
        var cargoIds = (await _cargoCompanyRepository.GetAllAsync())
            .Where(c => c.IsActive).Select(c => c.Id).ToHashSet();

        foreach (var row in request.Rows)
        {
            if (row.CompanyDirectoryId is { } dirId && !directoryIds.Contains(dirId))
                return OperationResult<ImportResult>.Fail(
                    $"{row.RowNumber}. satırdaki firma artık mevcut/aktif değil ('{row.CompanyName}'). " +
                    "Dosyayı yeniden analiz edin.");

            if (row.CargoCompanyId is { } cargoId && !cargoIds.Contains(cargoId))
                return OperationResult<ImportResult>.Fail(
                    $"{row.RowNumber}. satırdaki kargo firması artık mevcut/aktif değil ('{row.CargoCompanyName}'). " +
                    "Dosyayı yeniden analiz edin.");
        }

        // Takip numarası çakışması (kesin anahtar) yeniden kontrol edilir
        var trackingNumbers = request.Rows
            .Where(r => !string.IsNullOrWhiteSpace(r.TrackingNumber))
            .Select(r => r.TrackingNumber!.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (trackingNumbers.Count > 0)
        {
            var conflicts = await _repository.GetByTrackingNumbersAsync(request.Direction, trackingNumbers);
            if (conflicts.Count > 0)
                return OperationResult<ImportResult>.Fail(
                    "Önizlemeden sonra veritabanına aynı takip numaralı kayıt(lar) eklendi " +
                    $"({conflicts.Count} çakışma). Hiçbir kayıt oluşturulmadı — dosyayı yeniden analiz edin.");
        }

        // ── Entity üretimi (CreateCargoShipmentHandler eşleme kuralları) ──
        var entities  = new List<Domain.Entities.CargoShipment>(request.Rows.Count);
        var processed = 0;

        foreach (var row in request.Rows)
        {
            processed++;
            request.Progress?.Report(new ImportProgress("Kayıtlar hazırlanıyor", processed, request.Rows.Count));

            entities.Add(new Domain.Entities.CargoShipment
            {
                Id                 = Guid.NewGuid(),
                Direction          = request.Direction,
                ShipmentDate       = DateTime.SpecifyKind(row.ShipmentDate.Date, DateTimeKind.Utc),
                ShipmentType       = row.ShipmentType,
                Priority           = row.Priority,
                CreatedFrom        = CargoShipmentCreatedFrom.ExcelImport,
                CargoCompanyId     = row.CargoCompanyId,
                CompanyDirectoryId = row.CompanyDirectoryId,

                ReceiverCompanyNameSnapshot = row.ReceiverCompanyNameSnapshot?.Trim(),
                ReceiverAddressSnapshot     = row.ReceiverAddressSnapshot?.Trim(),
                ReceiverAttentionSnapshot   = _textNormalization.Normalize(row.ReceiverAttentionSnapshot),
                ReceiverCitySnapshot        = row.ReceiverCitySnapshot?.Trim(),
                ReceiverDistrictSnapshot    = row.ReceiverDistrictSnapshot?.Trim(),
                ReceiverPhoneSnapshot       = row.ReceiverPhoneSnapshot?.Trim(),
                ReceiverEmailSnapshot       = row.ReceiverEmailSnapshot?.Trim(),

                SenderName     = _textNormalization.Normalize(row.SenderName),
                ReceiverName   = _textNormalization.Normalize(row.ReceiverName),
                VehiclePlate   = _textNormalization.Normalize(row.VehiclePlate),
                TrackingNumber = row.TrackingNumber?.Trim(),

                // Başlangıç durumu manuel akış varsayılanlarıyla uyumlu:
                // giden kargo hazırlanmış, gelen kargo beklemede başlar
                Status             = request.Direction == CargoShipmentDirection.Incoming
                                       ? CargoShipmentStatus.Waiting
                                       : CargoShipmentStatus.Prepared,
                NotificationStatus = CargoNotificationStatus.NotNotified,
                Notes              = _textNormalization.Normalize(row.Notes),
                CreatedByUserId    = request.CreatedByUserId,
                CreatedAt          = DateTime.UtcNow,
                IsDeleted          = false
            });
        }

        // ── Tek transaction: ya tüm kayıtlar girer ya hiçbiri ──
        try
        {
            request.Progress?.Report(new ImportProgress("Veritabanına kaydediliyor", request.Rows.Count, request.Rows.Count));
            await _repository.AddRangeWithAutoNumberAsync(entities);
        }
        catch (Exception ex)
        {
            await _systemLog.LogErrorAsync("CargoImport",
                $"Toplu içe aktarma başarısız ({request.SourceName}, {entities.Count} satır). Hiçbir kayıt oluşturulmadı.",
                ex, source: nameof(ImportCargoShipmentsHandler));

            return OperationResult<ImportResult>.Fail(ex is DataStoreException
                ? ex.Message
                : "Kayıtlar oluşturulamadı — hiçbir satır içe aktarılmadı. Teknik ayrıntı Sistem Loglarına kaydedildi.");
        }

        // ── Audit: satır bazlı + özet. Audit hatası import'u geri almaz (AuditLogService hataya dayanıklı). ──
        var directionText = request.Direction == CargoShipmentDirection.Incoming ? "Gelen" : "Giden";

        foreach (var entity in entities)
        {
            await _auditLogService.WriteAsync(
                AuditAction.CargoShipmentCreated,
                _userContext.UserId, _userContext.FullName,
                "CargoShipment", entity.Id,
                null,
                $"Yön: {directionText} | No: {entity.ShipmentNumber} | Tarih: {entity.ShipmentDate:dd.MM.yyyy} | Kaynak: {request.SourceName}");
        }

        var importId = Guid.NewGuid();
        var first    = entities[0].ShipmentNumber;
        var last     = entities[^1].ShipmentNumber;

        await _auditLogService.WriteAsync(
            AuditAction.CargoImportCompleted,
            _userContext.UserId, _userContext.FullName,
            "CargoImport", importId,
            null,
            $"Dosya: {request.SourceName} | Yön: {directionText} | {entities.Count} kayıt | Numara: {first} – {last}");

        await _systemLog.LogInfoAsync("CargoImport",
            $"Toplu içe aktarma tamamlandı: {request.SourceName} → {entities.Count} kayıt ({first} – {last}).",
            source: nameof(ImportCargoShipmentsHandler));

        _cache.Invalidate();

        return OperationResult<ImportResult>.Ok(new ImportResult
        {
            ImportId            = importId,
            SourceName          = request.SourceName,
            Direction           = request.Direction,
            StartedAtUtc        = startedAt,
            CompletedAtUtc      = DateTime.UtcNow,
            TotalRows           = request.AnalysisTotalRows,
            ValidCount          = request.AnalysisValidCount,
            WarningCount        = request.AnalysisWarningCount,
            ErrorCount          = request.AnalysisErrorCount,
            DuplicateCount      = request.AnalysisDuplicateCount,
            RequestedCount      = request.Rows.Count,
            ImportedCount       = entities.Count,
            FirstShipmentNumber = first,
            LastShipmentNumber  = last,
            ImportedByUserId    = _userContext.UserId,
            ImportedByUserName  = _userContext.FullName
        });
    }
}
