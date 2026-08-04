using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts;

/// <summary>
/// Ortak mail rehberinin yazma yetkisi kuralı — WhatsAppContactPermissions ile
/// bilinçli olarak aynı: rehber ayrı bir izne sahip değildir, onu fiilen kullanan
/// akışların izinleri kabul edilir (kargo bildirimi hazırlayanlar ve firma rehberi
/// yöneticileri). Okuma tüm oturumlu kullanıcılara açıktır.
/// </summary>
public static class MailContactPermissions
{
    public static bool CanModify(IUserContext userContext) =>
        userContext.HasPermission(PermissionType.CanManageIncomingCargo)
        || userContext.HasPermission(PermissionType.CanManageOutgoingCargo)
        || userContext.HasPermission(PermissionType.CanManageCompanyDirectory);
}
