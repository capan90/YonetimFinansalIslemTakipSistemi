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

    public async Task<OperationResult<bool>> HandleAsync(MailSettingsDto dto, bool isPersonal = false)
    {
        if (!isPersonal && !_userContext.HasPermission(PermissionType.CanManageMailSettings))
            return OperationResult<bool>.Fail("Mail ayarlarını düzenleme yetkiniz bulunmamaktadır.");

        var userId = _userContext.UserId;
        var prefix = isPersonal ? $"UserMail:{userId}:" : "Mail:";

        await _repo.UpsertAsync(prefix + "SmtpHost",    dto.SmtpHost,            false, userId);
        await _repo.UpsertAsync(prefix + "SmtpPort",    dto.SmtpPort.ToString(), false, userId);
        await _repo.UpsertAsync(prefix + "EnableSsl",   dto.EnableSsl.ToString(), false, userId);
        await _repo.UpsertAsync(prefix + "SenderEmail", dto.SenderEmail,         false, userId);
        await _repo.UpsertAsync(prefix + "SenderName",  dto.SenderName,          false, userId);
        await _repo.UpsertAsync(prefix + "Username",    dto.Username,            false, userId);

        // Şifre boşsa mevcut değer korunur; doluysa DPAPI ile şifrelenip kaydedilir
        var changedFields = new List<string> { "SmtpHost", "SmtpPort", "EnableSsl", "SenderEmail", "SenderName", "Username" };
        if (!string.IsNullOrEmpty(dto.Password))
        {
            var encrypted = _protector.Protect(dto.Password);
            await _repo.UpsertAsync(prefix + "Password", encrypted, true, userId);
            changedFields.Add("Password=******");
        }

        await _auditLogService.WriteAsync(
            AuditAction.MailSettingsUpdated,
            _userContext.UserId, _userContext.FullName,
            "ApplicationSetting", null,
            null, $"{(isPersonal ? "Kişisel" : "Genel")} mail ayarları güncellendi: {string.Join(", ", changedFields)}");

        return OperationResult<bool>.Ok(true);
    }
}
