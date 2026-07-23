using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.CreateWhatsAppContact;

/// <summary>
/// Ortak rehbere kişi ekler. Telefon normalize edilir; aynı normalize numara
/// ikinci kez kaydedilemez. Soft delete edilmiş aynı numara varsa kayıt
/// yeniden aktifleştirilir — kullanıcı için en akıcı geri yükleme davranışı.
/// </summary>
public class CreateWhatsAppContactHandler
{
    private readonly IWhatsAppContactRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;
    private readonly IUserTextNormalizationService _textNormalization;

    public CreateWhatsAppContactHandler(
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

    public async Task<OperationResult<WhatsAppContactDto>> HandleAsync(CreateWhatsAppContactRequest request)
    {
        // Ortak rehber kargo bildirim akışının parçasıdır: yazma yetkisi,
        // kargo yönetimi veya firma rehberi yönetimi izinlerinden birini gerektirir
        if (!WhatsAppContactPermissions.CanModify(_userContext))
            return OperationResult<WhatsAppContactDto>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            return OperationResult<WhatsAppContactDto>.Fail("Ad Soyad / Kayıt Adı zorunludur.");

        // Telefon harf dönüşümüne girmez; yalnızca normalize edilir
        var phone = PhoneNumberNormalizer.NormalizeTr(request.Phone);
        if (phone is null)
            return OperationResult<WhatsAppContactDto>.Fail(
                "Geçerli bir Türkiye cep telefonu numarası giriniz (örn: 0532 123 45 67).");

        // Mükerrer kontrol soft delete kayıtları da kapsar
        var existing = await _repository.GetByPhoneAsync(phone, includeDeleted: true);
        if (existing is not null && !existing.IsDeleted)
            return OperationResult<WhatsAppContactDto>.Fail(
                $"Bu telefon numarası WhatsApp rehberinde zaten kayıtlıdır. (Kayıt: {existing.FullName})");

        var fullName    = _textNormalization.Normalize(request.FullName)!;
        var company     = _textNormalization.Normalize(request.Company);
        var description = _textNormalization.Normalize(request.Description);

        if (existing is not null)
        {
            // Soft delete edilmiş numara: yeni kayıt yerine mevcut kayıt geri yüklenir
            existing.FullName        = fullName;
            existing.Company         = company;
            existing.Description     = description;
            existing.IsActive        = true;
            existing.IsDeleted       = false;
            existing.DeletedAt       = null;
            existing.DeletedByUserId = null;
            existing.UpdatedByUserId = _userContext.UserId;
            existing.UpdatedAt       = DateTime.UtcNow;

            await _repository.UpdateAsync(existing);

            await _auditLogService.WriteAsync(
                AuditAction.WhatsAppContactUpdated,
                _userContext.UserId,
                _userContext.FullName,
                "WhatsAppContact", existing.Id,
                "Silinmiş kayıt", $"Geri yüklendi — Ad: {fullName} | Telefon: {phone}");

            return OperationResult<WhatsAppContactDto>.Ok(ToDto(existing));
        }

        var entity = new WhatsAppContact
        {
            Id              = Guid.NewGuid(),
            FullName        = fullName,
            Phone           = phone,
            Company         = company,
            Description     = description,
            IsActive        = true,
            CreatedByUserId = _userContext.UserId,
            CreatedAt       = DateTime.UtcNow,
            IsDeleted       = false
        };

        await _repository.AddAsync(entity);

        await _auditLogService.WriteAsync(
            AuditAction.WhatsAppContactCreated,
            _userContext.UserId,
            _userContext.FullName,
            "WhatsAppContact", entity.Id,
            null, $"Ad: {fullName} | Telefon: {phone} | Firma: {company ?? "-"}");

        return OperationResult<WhatsAppContactDto>.Ok(ToDto(entity));
    }

    private static WhatsAppContactDto ToDto(WhatsAppContact c) => new()
    {
        Id          = c.Id,
        FullName    = c.FullName,
        Phone       = c.Phone,
        Company     = c.Company,
        Description = c.Description,
        IsActive    = c.IsActive,
        CreatedAt   = c.CreatedAt
    };
}
