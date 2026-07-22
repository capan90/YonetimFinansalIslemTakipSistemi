using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Settings.UserPreferences;

/// <summary>
/// Aktif kullanıcının harf tercihini kaydeder (upsert) ve oturuma anında uygular.
/// Yalnızca bundan sonraki kayıtlar etkilenir; eski kayıtlar geriye dönük değiştirilmez.
/// </summary>
public class SaveUserPreferenceHandler
{
    private readonly IUserPreferenceRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserContext _userContext;
    private readonly IUserSession _userSession;

    public SaveUserPreferenceHandler(
        IUserPreferenceRepository repository,
        IAuditLogService auditLogService,
        IUserContext userContext,
        IUserSession userSession)
    {
        _repository      = repository;
        _auditLogService = auditLogService;
        _userContext     = userContext;
        _userSession     = userSession;
    }

    public async Task<OperationResult<bool>> HandleAsync(TextCasePreference newPreference)
    {
        if (_userContext.UserId == Guid.Empty)
            return OperationResult<bool>.Fail("Oturum bilgisi bulunamadı.");

        var existing = await _repository.GetByUserIdAsync(_userContext.UserId);
        var oldPreference = existing?.TextCase ?? TextCasePreference.Preserve;

        if (existing is null)
        {
            await _repository.AddAsync(new UserPreference
            {
                Id              = Guid.NewGuid(),
                UserId          = _userContext.UserId,
                TextCase        = newPreference,
                CreatedByUserId = _userContext.UserId,
                CreatedAt       = DateTime.UtcNow,
                IsDeleted       = false
            });
        }
        else
        {
            existing.TextCase        = newPreference;
            existing.UpdatedByUserId = _userContext.UserId;
            existing.UpdatedAt       = DateTime.UtcNow;
            await _repository.UpdateAsync(existing);
        }

        // Ayar değişikliği kritik kullanıcı aksiyonu — audit zorunlu
        await _auditLogService.WriteAsync(
            AuditAction.UserPreferenceUpdated,
            _userContext.UserId,
            _userContext.FullName,
            "UserPreference", existing?.Id,
            $"Harf Duyarlılığı: {Display(oldPreference)}",
            $"Harf Duyarlılığı: {Display(newPreference)}");

        // Oturuma anında uygula — yeniden giriş beklenmez
        _userSession.SetTextCasePreference(newPreference);

        return OperationResult<bool>.Ok(true);
    }

    /// <summary>Audit ve UI'da kullanılan Türkçe karşılıklar.</summary>
    public static string Display(TextCasePreference preference) => preference switch
    {
        TextCasePreference.Uppercase => "BÜYÜK HARF",
        TextCasePreference.Lowercase => "küçük harf",
        _                            => "Olduğu Gibi"
    };
}
