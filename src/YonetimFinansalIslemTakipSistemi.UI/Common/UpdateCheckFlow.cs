using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;

namespace YonetimFinansalIslemTakipSistemi.UI.Common;

/// <summary>
/// "Güncellemeleri Denetle" akışı — kullanıcının başlattığı manuel kontrol.
///
/// NEDEN VAR: Bu akış MainWindow ve Kargo Panosu'nda BİREBİR AYNI şekilde iki
/// kez yazılmıştı. Kabuk üçüncü giriş noktası oluyordu; kopyalamak yerine tek
/// yere alındı. Metinler, onay sırası ve kapanış gecikmesi aynen korundu.
///
/// Açılıştaki OTOMATİK kontrol ayrı bir iştir ve
/// <see cref="Services.StartupUpdateChecker"/>'da kalır — o, kullanıcıyı
/// bloklamadan bir kez çalışır.
/// </summary>
internal static class UpdateCheckFlow
{
    public static async Task RunAsync(IServiceProvider services, IDialogService dialogService)
    {
        var updateService = services.GetRequiredService<IUpdateService>();

        if (!updateService.IsClickOnceDeployment)
        {
            dialogService.ShowInfo("Güncelleme kontrolü yalnızca ClickOnce ile kurulu sürümde kullanılabilir.");
            return;
        }

        var result = await updateService.CheckForUpdateAsync();

        if (result.ErrorMessage == "io_error")
        {
            dialogService.ShowWarning("Güncelleme sunucusuna erişilemiyor. Ağ bağlantınızı kontrol edin.");
            return;
        }

        if (result.ErrorMessage is not null)
        {
            dialogService.ShowWarning("Güncelleme kontrolü sırasında beklenmeyen bir hata oluştu.");
            return;
        }

        if (!result.IsUpdateAvailable)
        {
            dialogService.ShowInfo($"Uygulamanız güncel.\nMevcut sürüm: v{result.CurrentVersion}");
            return;
        }

        if (!dialogService.ShowConfirmation(
                $"Yeni sürüm mevcut: v{result.LatestVersion}\nMevcut sürüm: v{result.CurrentVersion}\n\nŞimdi güncellemek ister misiniz?",
                "Güncelleme Mevcut"))
            return;

        if (!dialogService.ShowConfirmation(
                "Güncelleme başlatılacak ve uygulama kapatılacak.\nDevam etmek istiyor musunuz?",
                "Uygulama Kapatılıyor"))
            return;

        // LaunchInstaller başarısız olursa (dosya yok, shell hatası) Shutdown çağrılmaz.
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
