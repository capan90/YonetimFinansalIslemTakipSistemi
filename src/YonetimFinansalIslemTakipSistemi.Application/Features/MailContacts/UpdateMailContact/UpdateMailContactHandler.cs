using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts.UpdateMailContact;

/// <summary>
/// Mail rehberi kişisini günceller. Adres değişirse normalize edilip mükerrer
/// kontrolden geçirilir (kendi kaydı hariç).
/// </summary>
public class UpdateMailContactHandler
{
    private readonly IMailContactRepository        _repository;
    private readonly IAuditLogService              _auditLogService;
    private readonly IUserContext                  _userContext;
    private readonly IUserTextNormalizationService _textNormalization;

    public UpdateMailContactHandler(
        IMailContactRepository        repository,
        IAuditLogService              auditLogService,
        IUserContext                  userContext,
        IUserTextNormalizationService textNormalization)
    {
        _repository        = repository;
        _auditLogService   = auditLogService;
        _userContext       = userContext;
        _textNormalization = textNormalization;
    }

    public async Task<OperationResult<bool>> HandleAsync(UpdateMailContactRequest request)
    {
        if (!MailContactPermissions.CanModify(_userContext))
            return OperationResult<bool>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            return OperationResult<bool>.Fail("Ad Soyad / Kayıt Adı zorunludur.");

        if (!EmailAddressHelper.IsValid(request.Email))
            return OperationResult<bool>.Fail(
                "Geçerli bir e-posta adresi giriniz (örn: ornek@firma.com).");

        var email = EmailAddressHelper.Normalize(request.Email)!;

        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity is null)
            return OperationResult<bool>.Fail("Rehber kaydı bulunamadı.");

        // Aynı adrese sahip başka kayıt (silinmişler dahil) varsa engelle
        var duplicate = await _repository.GetByEmailAsync(email, includeDeleted: true);
        if (duplicate is not null && duplicate.Id != entity.Id)
            return OperationResult<bool>.Fail(
                $"Bu e-posta adresi mail rehberinde zaten kayıtlıdır. (Kayıt: {duplicate.FullName})");

        var oldValues = Describe(entity.FullName, entity.Email, entity.Company, entity.IsDefaultCc, entity.IsActive);

        entity.FullName        = _textNormalization.Normalize(request.FullName)!;
        entity.Email           = email;
        entity.Company         = _textNormalization.Normalize(request.Company);
        entity.Description     = _textNormalization.Normalize(request.Description);
        entity.IsDefaultCc     = request.IsDefaultCc;
        entity.IsActive        = request.IsActive;
        entity.UpdatedByUserId = _userContext.UserId;
        entity.UpdatedAt       = DateTime.UtcNow;

        await _repository.UpdateAsync(entity);

        await _auditLogService.WriteAsync(
            AuditAction.MailContactUpdated,
            _userContext.UserId,
            _userContext.FullName,
            "MailContact", entity.Id,
            oldValues,
            Describe(entity.FullName, entity.Email, entity.Company, entity.IsDefaultCc, entity.IsActive));

        return OperationResult<bool>.Ok(true);
    }

    private static string Describe(string name, string email, string? company, bool defaultCc, bool active)
        => $"Ad: {name} | E-posta: {email} | Firma: {company ?? "-"} | Varsayılan CC: {defaultCc} | Aktif: {active}";
}
