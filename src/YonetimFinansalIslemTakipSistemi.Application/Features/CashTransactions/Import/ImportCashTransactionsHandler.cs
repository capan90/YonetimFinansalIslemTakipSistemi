using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Import;

/// <summary>
/// Onaylanmış finans satırlarını TEK transaction'da kaydeder (ya hep ya hiç).
/// FİNANSAL VERİ: önizleme sonrası oluşmuş yeni mükerrerler tespit edilirse
/// tüm işlem iptal edilir — kullanıcının bilinçli dahil ettikleri hariç.
/// Bakiye hesapları kayıtlardan türetildiği için ek bakiye işlemi gerekmez;
/// liste yenilenince bakiye barı otomatik güncellenir.
/// </summary>
public class ImportCashTransactionsHandler
{
    private readonly ICashTransactionRepository _repository;
    private readonly IAuditLogService           _auditLogService;
    private readonly ISystemLogService          _systemLog;
    private readonly IUserContext               _userContext;

    public ImportCashTransactionsHandler(
        ICashTransactionRepository repository,
        IAuditLogService           auditLogService,
        ISystemLogService          systemLog,
        IUserContext               userContext)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _systemLog       = systemLog;
        _userContext     = userContext;
    }

    public async Task<OperationResult<CashImportResult>> HandleAsync(ImportCashTransactionsRequest request)
    {
        var startedAt = DateTime.UtcNow;

        if (!_userContext.HasPermission(PermissionType.CanCreateTransaction))
            return OperationResult<CashImportResult>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        if (request.Rows.Count == 0)
            return OperationResult<CashImportResult>.Fail("İçe aktarılacak satır seçilmedi.");

        if (request.CreatedByUserId == Guid.Empty)
            return OperationResult<CashImportResult>.Fail("İşlemi yapan kullanıcı belirtilmelidir.");

        if (request.Rows.Any(r => !r.CanInclude || r.Amount <= 0 || r.Description is null || r.TransactionDate == default))
            return OperationResult<CashImportResult>.Fail(
                "Hatalı satırlar içe aktarılamaz. Önizlemeyi yenileyin.");

        // Yeniden doğrulama: önizleme ile onay arasında başka kullanıcı aynı işlemi girmiş olabilir.
        // Kullanıcının önizlemede bilinçli dahil ettiği mükerrerler engellenmez.
        var pending = request.Rows.Where(r => r.DuplicateReason is null).ToList();
        if (pending.Count > 0)
        {
            var minDate = DateTime.SpecifyKind(pending.Min(r => r.TransactionDate), DateTimeKind.Utc);
            var maxDate = DateTime.SpecifyKind(pending.Max(r => r.TransactionDate), DateTimeKind.Utc);
            var existing = await _repository.GetFilteredAsync(minDate, maxDate, null, null);

            var dbKeys = existing.Select(t => new CashImportRowDto
            {
                RowNumber = 0, TransactionDate = t.TransactionDate.Date,
                TransactionType = t.TransactionType, CurrencyType = t.CurrencyType,
                Amount = t.Amount, Description = t.Description
            }.DuplicateKey).ToHashSet();

            var conflicts = pending.Count(r => dbKeys.Contains(r.DuplicateKey));
            if (conflicts > 0)
                return OperationResult<CashImportResult>.Fail(
                    $"Önizlemeden sonra sisteme aynı bilgilerle {conflicts} işlem girildi. " +
                    "Hiçbir kayıt oluşturulmadı — dosyayı yeniden analiz edin.");
        }

        // Entity üretimi — CreateCashTransactionHandler eşleme kurallarıyla aynı
        var entities  = new List<Domain.Entities.CashTransaction>(request.Rows.Count);
        var processed = 0;

        foreach (var row in request.Rows)
        {
            processed++;
            request.Progress?.Report(new ImportProgress("Kayıtlar hazırlanıyor", processed, request.Rows.Count));

            entities.Add(new Domain.Entities.CashTransaction
            {
                Id              = Guid.NewGuid(),
                // Npgsql timestamptz için UTC zorunlu
                TransactionDate = DateTime.SpecifyKind(row.TransactionDate.Date, DateTimeKind.Utc),
                TransactionType = row.TransactionType,
                CurrencyType    = row.CurrencyType,
                Amount          = row.Amount,
                Description     = row.Description!, // analiz aşamasında normalize edildi
                CreatedByUserId = request.CreatedByUserId,
                CreatedAt       = DateTime.UtcNow,
                IsDeleted       = false
            });
        }

        try
        {
            request.Progress?.Report(new ImportProgress("Veritabanına kaydediliyor", request.Rows.Count, request.Rows.Count));
            await _repository.AddRangeAsync(entities);
        }
        catch (Exception ex)
        {
            await _systemLog.LogErrorAsync("CashImport",
                $"Finans toplu içe aktarma başarısız ({request.SourceName}, {entities.Count} satır). Hiçbir kayıt oluşturulmadı.",
                ex, source: nameof(ImportCashTransactionsHandler));

            return OperationResult<CashImportResult>.Fail(
                "Kayıtlar oluşturulamadı — hiçbir satır içe aktarılmadı. Teknik ayrıntı Sistem Loglarına kaydedildi.");
        }

        // Audit toplu yazılır (satır bazlı + özet)
        var importId = Guid.NewGuid();
        request.Progress?.Report(new ImportProgress("Denetim kayıtları yazılıyor", entities.Count, entities.Count));

        var auditEntries = entities.Select(entity => new AuditEntry(
            AuditAction.TransactionCreated,
            _userContext.UserId, _userContext.FullName,
            "CashTransaction", entity.Id,
            null,
            $"Tarih: {entity.TransactionDate:dd.MM.yyyy} | Tip: {entity.TransactionType} | " +
            $"Para Birimi: {entity.CurrencyType} | Tutar: {entity.Amount.ToString("N2", new System.Globalization.CultureInfo("tr-TR"))} | " +
            $"Açıklama: {entity.Description} | Kaynak: {request.SourceName}"))
            .ToList();

        var girisCount = entities.Count(e => e.TransactionType == TransactionType.Giris);
        var cikisCount = entities.Count  - girisCount;

        auditEntries.Add(new AuditEntry(
            AuditAction.CashImportCompleted,
            _userContext.UserId, _userContext.FullName,
            "CashImport", importId,
            null,
            $"Dosya: {request.SourceName} | {entities.Count} işlem ({girisCount} giriş, {cikisCount} çıkış)"));

        await _auditLogService.WriteRangeAsync(auditEntries);

        await _systemLog.LogInfoAsync("CashImport",
            $"Finans toplu içe aktarma tamamlandı: {request.SourceName} → {entities.Count} işlem.",
            source: nameof(ImportCashTransactionsHandler));

        return OperationResult<CashImportResult>.Ok(new CashImportResult
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
            GirisCount         = girisCount,
            CikisCount         = cikisCount,
            ImportedByUserId   = _userContext.UserId,
            ImportedByUserName = _userContext.FullName
        });
    }
}
