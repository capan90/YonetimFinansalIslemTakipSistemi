using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.UpdateWhatsAppContact;

/// <summary>
/// Rehber kişisini günceller. Telefon değişirse normalize edilip
/// mükerrer kontrolden geçirilir (kendi kaydı hariç).
/// </summary>
public class UpdateWhatsAppContactHandler
{
    private readonly IWhatsAppContactRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;
    private readonly IUserTextNormalizationService _textNormalization;

    public UpdateWhatsAppContactHandler(
        IWhatsAppContactRepository repository,
        IAuditLogService auditLogService,
        IUserContext userContext,
        IUserTextNormalizationService textNormalization)
    {
        _repository        = repository;
        _auditLogService   = auditLogService;
        _userContext       = userContext;
        _textNormalization = textNormalization;
    }

    public async Task<OperationResult<bool>> HandleAsync(UpdateWhatsAppContactRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return OperationResult<bool>.Fail("Ad Soyad / Kayıt Adı zorunludur.");

        var phone = PhoneNumberNormalizer.NormalizeTr(request.Phone);
        if (phone is null)
            return OperationResult<bool>.Fail(
                "Geçerli bir Türkiye cep telefonu numarası giriniz (örn: 0532 123 45 67).");

        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity is null)
            return OperationResult<bool>.Fail("Rehber kaydı bulunamadı.");

        // Aynı numaraya sahip başka kayıt (silinmişler dahil) varsa engelle
        var duplicate = await _repository.GetByPhoneAsync(phone, includeDeleted: true);
        if (duplicate is not null && duplicate.Id != entity.Id)
            return OperationResult<bool>.Fail(
                $"Bu telefon numarası WhatsApp rehberinde zaten kayıtlıdır. (Kayıt: {duplicate.FullName})");

        var oldValues = $"Ad: {entity.FullName} | Telefon: {entity.Phone} | Firma: {entity.Company ?? "-"} | Aktif: {entity.IsActive}";

        entity.FullName        = _textNormalization.Normalize(request.FullName)!;
        entity.Phone           = phone;
        entity.Company         = _textNormalization.Normalize(request.Company);
        entity.Description     = _textNormalization.Normalize(request.Description);
        entity.IsActive        = request.IsActive;
        entity.UpdatedByUserId = _userContext.UserId;
        entity.UpdatedAt       = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);

        await _auditLogService.WriteAsync(
            AuditAction.WhatsAppContactUpdated,
            _userContext.UserId,
            _userContext.FullName,
            "WhatsAppContact", entity.Id,
            oldValues,
            $"Ad: {entity.FullName} | Telefon: {entity.Phone} | Firma: {entity.Company ?? "-"} | Aktif: {entity.IsActive}");

        return OperationResult<bool>.Ok(true);
    }
}
