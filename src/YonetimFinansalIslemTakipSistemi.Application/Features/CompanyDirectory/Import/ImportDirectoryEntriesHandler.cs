using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Import;

/// <summary>
/// Onaylanmış rehber satırlarını TEK transaction'da kaydeder (ya hep ya hiç).
/// Adres boşsa "-" yazılır: kolon veritabanında zorunlu, ancak taşınan telefon
/// rehberinde adres verisi yok — migration yerine görünür bir yer tutucu tercih edildi.
/// </summary>
public class ImportDirectoryEntriesHandler
{
    private readonly ICompanyDirectoryRepository   _repository;
    private readonly IAuditLogService              _auditLogService;
    private readonly ISystemLogService             _systemLog;
    private readonly IUserContext                  _userContext;
    private readonly IUserTextNormalizationService _textNormalization;

    public ImportDirectoryEntriesHandler(
        ICompanyDirectoryRepository   repository,
        IAuditLogService              auditLogService,
        ISystemLogService             systemLog,
        IUserContext                  userContext,
        IUserTextNormalizationService textNormalization)
    {
        _repository        = repository;
        _auditLogService   = auditLogService;
        _systemLog         = systemLog;
        _userContext       = userContext;
        _textNormalization = textNormalization;
    }

    public async Task<OperationResult<DirectoryImportResult>> HandleAsync(ImportDirectoryEntriesRequest request)
    {
        var startedAt = DateTime.UtcNow;

        if (!_userContext.HasPermission(PermissionType.CanManageCompanyDirectory))
            return OperationResult<DirectoryImportResult>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        if (request.Rows.Count == 0)
            return OperationResult<DirectoryImportResult>.Fail("İçe aktarılacak satır seçilmedi.");

        if (request.CreatedByUserId == Guid.Empty)
            return OperationResult<DirectoryImportResult>.Fail("İşlemi yapan kullanıcı belirtilmelidir.");

        if (request.Rows.Any(r => !r.CanInclude))
            return OperationResult<DirectoryImportResult>.Fail(
                "Hatalı satırlar içe aktarılamaz. Önizlemeyi yenileyin.");

        // Yeniden doğrulama: önizleme sonrası başka kullanıcı aynı ad+telefonla firma
        // eklemiş olabilir (anahtar analizle aynı — aynı ad farklı numara serbesttir).
        // Kullanıcının bilinçli dahil ettiği (önizlemede işaretli) mükerrerler engellenmez.
        var existingKeys = (await _repository.GetAllAsync())
            .Select(d => DirectoryDuplicateKey.Build(d.CompanyName, d.Phone))
            .ToHashSet();

        var newConflicts = request.Rows.Count(r =>
            r.DuplicateReason is null &&
            r.CompanyName is not null &&
            existingKeys.Contains(DirectoryDuplicateKey.Build(r.CompanyName, r.Phone)));

        if (newConflicts > 0)
            return OperationResult<DirectoryImportResult>.Fail(
                $"Önizlemeden sonra rehbere aynı ad ve telefonla {newConflicts} firma eklendi. " +
                "Hiçbir kayıt oluşturulmadı — dosyayı yeniden analiz edin.");

        var entities  = new List<Domain.Entities.CompanyDirectory>(request.Rows.Count);
        var processed = 0;

        foreach (var row in request.Rows)
        {
            processed++;
            request.Progress?.Report(new ImportProgress("Kayıtlar hazırlanıyor", processed, request.Rows.Count));

            entities.Add(new Domain.Entities.CompanyDirectory
            {
                Id            = Guid.NewGuid(),
                // CreateCompanyDirectoryHandler ile aynı eşleme kuralları
                CompanyName   = _textNormalization.Normalize(row.CompanyName) ?? string.Empty,
                ContactPerson = _textNormalization.Normalize(row.ContactPerson),
                AttentionTo   = _textNormalization.Normalize(row.AttentionTo),
                AddressLine   = _textNormalization.Normalize(row.AddressLine) ?? "-",
                District      = _textNormalization.Normalize(row.District),
                City          = _textNormalization.Normalize(row.City),
                PostalCode    = row.PostalCode?.Trim(),
                Phone         = row.Phone?.Trim(),
                Email         = row.Email?.Trim()?.ToLowerInvariant(),
                Notes         = _textNormalization.Normalize(row.Notes),
                IsActive      = true,
                CreatedByUserId = request.CreatedByUserId,
                CreatedAt     = DateTime.UtcNow,
                IsDeleted     = false
            });
        }

        try
        {
            request.Progress?.Report(new ImportProgress("Veritabanına kaydediliyor", request.Rows.Count, request.Rows.Count));
            await _repository.AddRangeAsync(entities);
        }
        catch (Exception ex)
        {
            await _systemLog.LogErrorAsync("DirectoryImport",
                $"Rehber toplu içe aktarma başarısız ({request.SourceName}, {entities.Count} satır). Hiçbir kayıt oluşturulmadı.",
                ex, source: nameof(ImportDirectoryEntriesHandler));

            return OperationResult<DirectoryImportResult>.Fail(
                "Kayıtlar oluşturulamadı — hiçbir satır içe aktarılmadı. Teknik ayrıntı Sistem Loglarına kaydedildi.");
        }

        // Audit toplu yazılır — kayıt başına round-trip binlerce satırda UI'ı dondurur
        var importId = Guid.NewGuid();
        request.Progress?.Report(new ImportProgress("Denetim kayıtları yazılıyor", entities.Count, entities.Count));

        var auditEntries = entities.Select(entity => new AuditEntry(
            AuditAction.CompanyDirectoryCreated,
            _userContext.UserId, _userContext.FullName,
            "CompanyDirectory", entity.Id,
            null, $"Firma: {entity.CompanyName} | Adres: {entity.AddressLine} | Kaynak: {request.SourceName}"))
            .ToList();

        auditEntries.Add(new AuditEntry(
            AuditAction.DirectoryImportCompleted,
            _userContext.UserId, _userContext.FullName,
            "DirectoryImport", importId,
            null, $"Dosya: {request.SourceName} | {entities.Count} firma kaydı"));

        await _auditLogService.WriteRangeAsync(auditEntries);

        await _systemLog.LogInfoAsync("DirectoryImport",
            $"Rehber toplu içe aktarma tamamlandı: {request.SourceName} → {entities.Count} firma.",
            source: nameof(ImportDirectoryEntriesHandler));

        return OperationResult<DirectoryImportResult>.Ok(new DirectoryImportResult
        {
            ImportId           = importId,
            SourceName         = request.SourceName,
            StartedAtUtc       = startedAt,
            CompletedAtUtc     = DateTime.UtcNow,
            TotalRows          = request.AnalysisTotalRows,
            ValidCount         = request.AnalysisValidCount,
            WarningCount       = request.AnalysisWarningCount,
            ErrorCount         = request.AnalysisErrorCount,
            DuplicateCount     = request.AnalysisDuplicateCount,
            RequestedCount     = request.Rows.Count,
            ImportedCount      = entities.Count,
            ImportedByUserId   = _userContext.UserId,
            ImportedByUserName = _userContext.FullName
        });
    }
}
