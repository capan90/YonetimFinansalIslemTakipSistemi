using System.Windows;
using System.Windows.Controls;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Shell;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Kabuk testlerinin ortak sahte nesneleri ve kurulum yardımcıları.
///
/// NEDEN VAR: aynı FakeUserContext / ekran fabrikası dört ayrı test
/// dosyasında kopyalanmıştı. Kopyalar birbirinden habersiz kaydığında test
/// yeşil kalır ama farklı şeyleri sınar — sessiz bir kayıp.
///
/// Ekran listesi <see cref="ShellViewModel"/>'e DIŞARIDAN verildiği için
/// kabuk mantığı gerçek ekranlara bağımlı olmadan sınanabiliyor.
/// </summary>
public static class ShellTestDoubles
{
    public sealed class FakeUserContext(params PermissionType[] permissions) : IUserContext
    {
        public Guid   UserId   => Guid.Empty;
        public string FullName => "Test Kullanıcı";
        public TextCasePreference TextCasePreference => TextCasePreference.Preserve;
        public IReadOnlySet<PermissionType> Permissions { get; } = permissions.ToHashSet();
        public bool HasPermission(PermissionType permission) => Permissions.Contains(permission);
    }

    public sealed class FakeDialogService : IDialogService
    {
        public void ShowInfo(string message, string title = "Bilgi") { }
        public void ShowSuccess(string message, string title = "Başarılı") { }
        public void ShowWarning(string message, string title = "Uyarı") { }
        public void ShowError(string message, string title = "Hata") { }
        public bool ShowConfirmation(string message, string title = "Onay") => true;
    }

    /// <summary>
    /// Açılışta güncelleme denetimi yapmayan sahte servis. Kabuk GÖSTERİLDİĞİNDE
    /// (Loaded) bu servisi ister; gerçek olanı testte ağa/yayın klasörüne
    /// giderdi.
    /// </summary>
    public sealed class FakeUpdateService : IUpdateService
    {
        public bool IsClickOnceDeployment => false;

        public Task<UpdateCheckResult> CheckForUpdateAsync() =>
            Task.FromResult(new UpdateCheckResult(false, null, null, null));

        public bool LaunchInstaller() => false;
    }

    public sealed class FakeServices(IUserContext userContext) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IUserContext))   return userContext;
            if (serviceType == typeof(IDialogService)) return new FakeDialogService();
            if (serviceType == typeof(IUpdateService)) return new FakeUpdateService();
            return null;
        }
    }

    /// <summary>Kapanmayı reddedebilen ekran — kaydedilmemiş değişiklik benzetimi.</summary>
    public sealed class BlockingScreen : UserControl, IShellScreen
    {
        public bool AllowClose    { get; set; }
        public int  CloseAttempts { get; private set; }

        public bool RequestClose()
        {
            CloseAttempts++;
            return AllowClose;
        }
    }

    /// <summary>Testin kendi ekran tanımı. Varsayılan hâli her zaman açılabilir.</summary>
    public static ScreenDefinition Screen(
        ScreenKey key,
        PermissionType? permission = null,
        bool migrated = true,
        bool canClose = true,
        Func<IServiceProvider, FrameworkElement>? factory = null)
        => new(key, key.ToString(),
               permission is null ? [] : [permission.Value],
               migrated ? factory ?? (_ => new UserControl()) : null,
               CreateInstance: null,
               IsParameterized: false,
               CanClose: canClose);

    /// <summary>
    /// Yetki kapısı olmayan ekranlarla kurulmuş kabuk — sekme davranışını
    /// sınayan testler yetkiyle uğraşmasın diye.
    /// </summary>
    public static ShellViewModel Shell(params ScreenDefinition[] screens)
    {
        var userContext = new FakeUserContext();
        return new ShellViewModel(new FakeServices(userContext), userContext, screens);
    }

    /// <summary>Yetkiye bağlı kurulum — görünürlük ve erişim testleri için.</summary>
    public static ShellViewModel Shell(
        IReadOnlyList<ScreenDefinition> screens,
        params PermissionType[] permissions)
    {
        var userContext = new FakeUserContext(permissions);
        return new ShellViewModel(new FakeServices(userContext), userContext, screens);
    }
}
