using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Settings.MailSettings;

public class GetMailSettingsHandler
{
    private readonly IMailSettingsService _mailSettingsService;
    private readonly IUserContext         _userContext;

    public GetMailSettingsHandler(IMailSettingsService mailSettingsService, IUserContext userContext)
    {
        _mailSettingsService = mailSettingsService;
        _userContext         = userContext;
    }

    public async Task<OperationResult<MailSettingsDto>> HandleAsync(bool isPersonal = false)
    {
        if (!isPersonal && !_userContext.HasPermission(PermissionType.CanManageMailSettings))
            return OperationResult<MailSettingsDto>.Fail("Mail ayarlarını görüntüleme yetkiniz bulunmamaktadır.");

        var settings = isPersonal
            ? await _mailSettingsService.GetPersonalOnlyAsync(_userContext.UserId)
            : await _mailSettingsService.GetGlobalAsync();

        // Şifreyi UI'ya açık göndermiyoruz
        if (settings is not null)
            settings.Password = "";

        return OperationResult<MailSettingsDto>.Ok(settings ?? new MailSettingsDto());
    }
}
