using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts.GetMailContactList;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts.CreateMailContact;

/// <summary>
/// Ortak mail rehberine kişi ekler. E-posta normalize edilir; aynı adres ikinci kez
/// kaydedilemez. Soft delete edilmiş aynı adres varsa kayıt yeniden aktifleştirilir —
/// WhatsApp rehberiyle aynı geri yükleme davranışı.
/// </summary>
public class CreateMailContactHandler
{
    private readonly IMailContactRepository        _repository;
    private readonly IAuditLogService              _auditLogService;
    private readonly IUserContext                  _userContext;
    private readonly IUserTextNormalizationService _textNormalization;

    public CreateMailContactHandler(
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

    public async Task<OperationResult<MailContactDto>> HandleAsync(CreateMailContactRequest request)
    {
        if (!MailContactPermissions.CanModify(_userContext))
            return OperationResult<MailContactDto>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            return OperationResult<MailContactDto>.Fail("Ad Soyad / Kayıt Adı zorunludur.");

        // E-posta harf tercihine tabi değildir; yalnızca normalize edilir
        if (!EmailAddressHelper.IsValid(request.Email))
            return OperationResult<MailContactDto>.Fail(
                "Geçerli bir e-posta adresi giriniz (örn: ornek@firma.com).");

        var email = EmailAddressHelper.Normalize(request.Email)!;

        // Mükerrer kontrol soft delete kayıtları da kapsar
        var existing = await _repository.GetByEmailAsync(email, includeDeleted: true);
        if (existing is not null && !existing.IsDeleted)
            return OperationResult<MailContactDto>.Fail(
                $"Bu e-posta adresi mail rehberinde zaten kayıtlıdır. (Kayıt: {existing.FullName})");

        var fullName    = _textNormalization.Normalize(request.FullName)!;
        var company     = _textNormalization.Normalize(request.Company);
        var description = _textNormalization.Normalize(request.Description);

        if (existing is not null)
        {
            // Soft delete edilmiş adres: yeni kayıt yerine mevcut kayıt geri yüklenir
            existing.FullName        = fullName;
            existing.Company         = company;
            existing.Description     = description;
            existing.IsDefaultCc     = request.IsDefaultCc;
            existing.IsActive        = true;
            existing.IsDeleted       = false;
            existing.DeletedAt       = null;
            existing.DeletedByUserId = null;
            existing.UpdatedByUserId = _userContext.UserId;
            existing.UpdatedAt       = DateTime.UtcNow;

            await _repository.UpdateAsync(existing);

            await _auditLogService.WriteAsync(
                AuditAction.MailContactUpdated,
                _userContext.UserId,
                _userContext.FullName,
                "MailContact", existing.Id,
                "Silinmiş kayıt", $"Geri yüklendi — Ad: {fullName} | E-posta: {email}");

            return OperationResult<MailContactDto>.Ok(GetMailContactListHandler.ToDto(existing));
        }

        var entity = new MailContact
        {
            Id              = Guid.NewGuid(),
            FullName        = fullName,
            Email           = email,
            Company         = company,
            Description     = description,
            IsDefaultCc     = request.IsDefaultCc,
            IsActive        = true,
            CreatedByUserId = _userContext.UserId,
            CreatedAt       = DateTime.UtcNow,
            IsDeleted       = false
        };

        await _repository.AddAsync(entity);

        await _auditLogService.WriteAsync(
            AuditAction.MailContactCreated,
            _userContext.UserId,
            _userContext.FullName,
            "MailContact", entity.Id,
            null,
            $"Ad: {fullName} | E-posta: {email} | Firma: {company ?? "-"} | Varsayılan CC: {request.IsDefaultCc}");

        return OperationResult<MailContactDto>.Ok(GetMailContactListHandler.ToDto(entity));
    }
}
