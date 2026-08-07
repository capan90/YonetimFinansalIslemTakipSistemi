using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;

namespace YonetimFinansalIslemTakipSistemi.UI.Common;

/// <summary>
/// Oturum kapatmanın kabuk penceresinden bağımsız parçaları.
///
/// NEDEN VAR: Çıkış akışı artık birden fazla kabuk penceresinden başlıyor
/// (MainWindow ve ShellWindow). Onay metni ve audit kaydı her pencerede
/// yeniden yazılırsa biri düzeltildiğinde diğeri eski davranışta kalır —
/// ve bu, denetim kaydını sessizce tutarsızlaştırır.
///
/// Pencerenin KENDİ işi olan kısımlar burada DEĞİL: <c>IsLogoutRequested</c>
/// ve <c>Close()</c> her pencerede kalır, çünkü App.xaml.cs'in okuduğu
/// sözleşme odur.
/// </summary>
internal static class SessionLogout
{
    /// <summary>Kullanıcıya çıkış onayı sorar.</summary>
    public static bool Confirm(IDialogService dialogService)
        => dialogService.ShowConfirmation("Oturumu kapatmak istediğinize emin misiniz?", "Çıkış Yap");

    /// <summary>
    /// UserLoggedOut denetim kaydını yazar.
    ///
    /// Pencere (ve onunla birlikte DbContext scope'u) KAPANMADAN önce
    /// çağrılmalı. Audit yazılamazsa çıkış engellenmez — kullanıcı oturumdan
    /// çıkamamış olmaz; kritik hatalar global handler'da loglanır.
    /// </summary>
    public static async Task WriteAuditAsync(IServiceProvider services)
    {
        try
        {
            var userContext = services.GetRequiredService<IUserContext>();
            await services.GetRequiredService<IAuditLogService>().WriteAsync(
                AuditAction.UserLoggedOut, userContext.UserId, userContext.FullName,
                "User", userContext.UserId);
        }
        catch { /* audit hatası çıkışı engellemez */ }
    }
}
