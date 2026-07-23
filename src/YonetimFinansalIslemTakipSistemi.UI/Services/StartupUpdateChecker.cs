using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;

namespace YonetimFinansalIslemTakipSistemi.UI.Services;

/// <summary>
/// Açılışta bir kez çalışan güncelleme kontrolü (CLAUDE.md update kuralı).
/// dotnet-mage ile üretilen manifest'lerde ClickOnce Foreground aboneliği
/// bulunmadığından startup kontrolü uygulama içinden yapılır.
/// Sessiz politika: hata veya "zaten güncel" durumunda kullanıcı rahatsız edilmez;
/// yalnızca gerçek bir güncelleme varsa onay istenir (manuel akışla aynı diyaloglar).
/// </summary>
public static class StartupUpdateChecker
{
    private static bool _hasRun;

    public static async Task RunOnceAsync(IServiceProvider services, IDialogService dialogService)
    {
        // Süreç başına tek kontrol — logout/login döngüsünde tekrarlanmaz
        if (_hasRun) return;
        _hasRun = true;

        var updateService = services.GetRequiredService<IUpdateService>();
        if (!updateService.IsClickOnceDeployment)
            return;

        var result = await updateService.CheckForUpdateAsync();

        // Ağ/parse hataları UpdateService içinde loglanır; startup'ta kullanıcıya gösterilmez
        if (result.ErrorMessage is not null || !result.IsUpdateAvailable)
            return;

        if (!dialogService.ShowConfirmation(
                $"Yeni sürüm mevcut: v{result.LatestVersion}\nMevcut sürüm: v{result.CurrentVersion}\n\nŞimdi güncellemek ister misiniz?",
                "Güncelleme Mevcut"))
            return;

        if (!dialogService.ShowConfirmation(
                "Güncelleme başlatılacak ve uygulama kapatılacak.\nDevam etmek istiyor musunuz?",
                "Uygulama Kapatılıyor"))
            return;

        if (!updateService.LaunchInstaller())
        {
            dialogService.ShowError(
                "Güncelleme başlatılamadı. Güncelleme sunucusuna erişilemiyor veya kurulum dosyası bulunamadı.");
            return;
        }

        // Yeni sürecin spawn olması için kısa bekleme; ardından eski sürüm güvenle kapanır.
        await Task.Delay(800);
        System.Windows.Application.Current.Shutdown();
    }
}
