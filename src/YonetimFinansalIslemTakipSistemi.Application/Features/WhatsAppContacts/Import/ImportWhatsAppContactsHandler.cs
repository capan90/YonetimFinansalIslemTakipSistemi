using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.Import;

/// <summary>
/// Onaylanmış WhatsApp kişilerini TEK transaction'da kaydeder (ya hep ya hiç).
/// Soft-delete kayıtta bulunan numaralar geri yüklenir (create akışıyla aynı kural).
/// Önizleme sonrası aynı numarayla aktif kayıt eklendiyse tüm işlem iptal edilir.
/// </summary>
public class ImportWhatsAppContactsHandler
{
    private readonly IWhatsAppContactRepository    _repository;
    private readonly IAuditLogService              _auditLogService;
    private readonly ISystemLogService             _systemLog;
    private readonly IUserContext                  _userContext;
    private readonly IUserTextNormalizationService _textNormalization;

    public ImportWhatsAppContactsHandler(
        IWhatsAppContactRepository    repository,
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

    public async Task<OperationResult<WhatsAppImportResult>> HandleAsync(ImportWhatsAppContactsRequest request)
    {
        var startedAt = DateTime.UtcNow;

        if (!WhatsAppContactPermissions.CanModify(_userContext))
            return OperationResult<WhatsAppImportResult>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        if (request.Rows.Count == 0)
            return OperationResult<WhatsAppImportResult>.Fail("İçe aktarılacak satır seçilmedi.");

        if (request.CreatedByUserId == Guid.Empty)
            return OperationResult<WhatsAppImportResult>.Fail("İşlemi yapan kullanıcı belirtilmelidir.");

        if (request.Rows.Any(r => !r.CanInclude || r.NormalizedPhone is null || r.FullName is null))
            return OperationResult<WhatsAppImportResult>.Fail(
                "Hatalı veya mükerrer satırlar içe aktarılamaz. Önizlemeyi yenileyin.");

        // Yeniden doğrulama: önizleme ile onay arasında rehber değişmiş olabilir
        var existingByPhone = new Dictionary<string, Domain.Entities.WhatsAppContact>();
        foreach (var contact in await _repository.GetAllForImportAsync())
            existingByPhone.TryAdd(contact.Phone, contact);

        var toAdd    = new List<Domain.Entities.WhatsAppContact>();
        var toUpdate = new List<Domain.Entities.WhatsAppContact>();
        var processed = 0;

        foreach (var row in request.Rows)
        {
            processed++;
            request.Progress?.Report(new ImportProgress("Kayıtlar hazırlanıyor", processed, request.Rows.Count));

            var fullName    = _textNormalization.Normalize(row.FullName)!;
            var company     = _textNormalization.Normalize(row.Company);
            var description = _textNormalization.Normalize(row.Description);

            if (existingByPhone.TryGetValue(row.NormalizedPhone!, out var existing))
            {
                if (!existing.IsDeleted)
                    return OperationResult<WhatsAppImportResult>.Fail(
                        $"Önizlemeden sonra rehbere aynı numarayla kayıt eklendi ({row.RowNumber}. satır). " +
                        "Hiçbir kayıt oluşturulmadı — dosyayı yeniden analiz edin.");

                // Soft delete geri yükleme — CreateWhatsAppContactHandler ile aynı alan seti
                existing.FullName        = fullName;
                existing.Company         = company;
                existing.Description     = description;
                existing.IsActive        = true;
                existing.IsDeleted       = false;
                existing.DeletedAt       = null;
                existing.DeletedByUserId = null;
                existing.UpdatedByUserId = request.CreatedByUserId;
                existing.UpdatedAt       = DateTime.UtcNow;
                toUpdate.Add(existing);
            }
            else
            {
                toAdd.Add(new Domain.Entities.WhatsAppContact
                {
                    Id              = Guid.NewGuid(),
                    FullName        = fullName,
                    Phone           = row.NormalizedPhone!,
                    Company         = company,
                    Description     = description,
                    IsActive        = true,
                    CreatedByUserId = request.CreatedByUserId,
                    CreatedAt       = DateTime.UtcNow,
                    IsDeleted       = false
                });
            }
        }

        try
        {
            request.Progress?.Report(new ImportProgress("Veritabanına kaydediliyor", request.Rows.Count, request.Rows.Count));
            await _repository.SaveImportAsync(toAdd, toUpdate);
        }
        catch (Exception ex)
        {
            await _systemLog.LogErrorAsync("WhatsAppImport",
                $"WhatsApp rehberi toplu içe aktarma başarısız ({request.SourceName}). Hiçbir kayıt oluşturulmadı.",
                ex, source: nameof(ImportWhatsAppContactsHandler));

            return OperationResult<WhatsAppImportResult>.Fail(
                "Kayıtlar oluşturulamadı — hiçbir satır içe aktarılmadı. Teknik ayrıntı Sistem Loglarına kaydedildi.");
        }

        foreach (var entity in toAdd)
            await _auditLogService.WriteAsync(
                AuditAction.WhatsAppContactCreated,
                _userContext.UserId, _userContext.FullName,
                "WhatsAppContact", entity.Id,
                null, $"Ad: {entity.FullName} | Telefon: {entity.Phone} | Kaynak: {request.SourceName}");

        foreach (var entity in toUpdate)
            await _auditLogService.WriteAsync(
                AuditAction.WhatsAppContactUpdated,
                _userContext.UserId, _userContext.FullName,
                "WhatsAppContact", entity.Id,
                "Silinmiş kayıt", $"Geri yüklendi — Ad: {entity.FullName} | Telefon: {entity.Phone} | Kaynak: {request.SourceName}");

        var importId = Guid.NewGuid();
        await _auditLogService.WriteAsync(
            AuditAction.WhatsAppImportCompleted,
            _userContext.UserId, _userContext.FullName,
            "WhatsAppImport", importId,
            null, $"Dosya: {request.SourceName} | {toAdd.Count + toUpdate.Count} kişi ({toUpdate.Count} geri yükleme)");

        await _systemLog.LogInfoAsync("WhatsAppImport",
            $"WhatsApp rehberi toplu içe aktarma tamamlandı: {request.SourceName} → {toAdd.Count} yeni, {toUpdate.Count} geri yükleme.",
            source: nameof(ImportWhatsAppContactsHandler));

        return OperationResult<WhatsAppImportResult>.Ok(new WhatsAppImportResult
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
            ImportedCount      = toAdd.Count + toUpdate.Count,
            ResurrectedCount   = toUpdate.Count,
            ImportedByUserId   = _userContext.UserId,
            ImportedByUserName = _userContext.FullName
        });
    }
}
