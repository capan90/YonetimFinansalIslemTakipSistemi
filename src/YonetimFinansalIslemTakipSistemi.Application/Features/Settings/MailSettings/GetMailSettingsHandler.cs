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

    public async Task<OperationResult<MailSettingsDto>> HandleAsync()
    {
        if (!_userContext.HasPermission(PermissionType.CanManageMailSettings))
            return OperationResult<MailSettingsDto>.Fail("Mail ayarlarını görüntüleme yetkiniz bulunmamaktadır.");

        var settings = await _mailSettingsService.GetAsync();

        // Şifreyi UI'ya açık göndermiyoruz
        if (settings is not null)
            settings.Password = "";

        return OperationResult<MailSettingsDto>.Ok(settings ?? new MailSettingsDto());
    }
}
