using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Settings.MailSettings;

public class SaveMailSettingsHandler
{
    private readonly IApplicationSettingRepository _repo;
    private readonly ISecretProtector              _protector;
    private readonly IAuditLogService              _auditLogService;
    private readonly IUserContext                  _userContext;

    public SaveMailSettingsHandler(
        IApplicationSettingRepository repo,
        ISecretProtector              protector,
        IAuditLogService              auditLogService,
        IUserContext                  userContext)
    {
        _repo            = repo;
        _protector       = protector;
        _auditLogService = auditLogService;
        _userContext     = userContext;
    }

    public async Task<OperationResult<bool>> HandleAsync(MailSettingsDto dto)
    {
        if (!_userContext.HasPermission(PermissionType.CanManageMailSettings))
            return OperationResult<bool>.Fail("Mail ayarlarını düzenleme yetkiniz bulunmamaktadır.");

        var userId = _userContext.UserId;

        await _repo.UpsertAsync("Mail:SmtpHost",    dto.SmtpHost,            false, userId);
        await _repo.UpsertAsync("Mail:SmtpPort",    dto.SmtpPort.ToString(), false, userId);
        await _repo.UpsertAsync("Mail:EnableSsl",   dto.EnableSsl.ToString(), false, userId);
        await _repo.UpsertAsync("Mail:SenderEmail", dto.SenderEmail,         false, userId);
        await _repo.UpsertAsync("Mail:SenderName",  dto.SenderName,          false, userId);
        await _repo.UpsertAsync("Mail:Username",    dto.Username,            false, userId);

        // Şifre boşsa mevcut değer korunur; doluysa DPAPI ile şifrelenip kaydedilir
        var changedFields = new List<string> { "SmtpHost", "SmtpPort", "EnableSsl", "SenderEmail", "SenderName", "Username" };
        if (!string.IsNullOrEmpty(dto.Password))
        {
            var encrypted = _protector.Protect(dto.Password);
            await _repo.UpsertAsync("Mail:Password", encrypted, true, userId);
            changedFields.Add("Password=******");
        }

        await _auditLogService.WriteAsync(
            AuditAction.MailSettingsUpdated,
            _userContext.UserId, _userContext.FullName,
            "ApplicationSetting", null,
            null, $"Mail ayarları güncellendi: {string.Join(", ", changedFields)}");

        return OperationResult<bool>.Ok(true);
    }
}
