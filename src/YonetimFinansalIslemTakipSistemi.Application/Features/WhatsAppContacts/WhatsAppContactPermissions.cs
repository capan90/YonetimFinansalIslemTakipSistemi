using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts;

/// <summary>
/// Ortak WhatsApp rehberinin yazma yetkisi kuralı — tek noktadan yönetilir.
/// Rehber ayrı bir izne sahip değildir (mevcut enum'a değer eklemek migration
/// gerektirmez ama izin ekranı/atama akışını büyütür); bunun yerine rehberi
/// fiilen kullanan akışların izinleri kabul edilir: kargo bildirimi hazırlayanlar
/// ve firma rehberi yöneticileri. Okuma (liste) tüm oturumlu kullanıcılara açıktır.
/// </summary>
public static class WhatsAppContactPermissions
{
    public static bool CanModify(IUserContext userContext) =>
        userContext.HasPermission(PermissionType.CanManageIncomingCargo)
        || userContext.HasPermission(PermissionType.CanManageOutgoingCargo)
        || userContext.HasPermission(PermissionType.CanManageCompanyDirectory);
}
