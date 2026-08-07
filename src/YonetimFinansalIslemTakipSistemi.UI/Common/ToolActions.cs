using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;

namespace YonetimFinansalIslemTakipSistemi.UI.Common;

/// <summary>
/// Ekran açmayan, sekmeye dönüşmeyen yardımcı eylemler.
///
/// NEDEN VAR: Bunlar menü çubuğunun "iş yapan" öğeleriydi ve MainWindow ile
/// Kargo Panosu'nda ayrı ayrı yazılmıştı. Kabuk üçüncü giriş noktası olunca
/// kopya sayısı üçe çıkacaktı; tek yere alındı. Mesaj metinleri ve hata
/// davranışı aynen korundu.
/// </summary>
internal static class ToolActions
{
    /// <summary>Veritabanı bağlantısını hemen test eder ve sonucu gösterir.</summary>
    public static async Task TestDatabaseAsync(IServiceProvider services, IDialogService dialogService)
    {
        var testService = services.GetRequiredService<IDatabaseConnectionTestService>();

        if (await testService.CanConnectAsync())
            dialogService.ShowSuccess("Veritabanı bağlantısı başarılı.");
        else
            dialogService.ShowError(
                "Veritabanı bağlantısı kurulamadı.\nLütfen ağ bağlantınızı veya sunucu erişimini kontrol edin.");
    }

    /// <summary>
    /// Log klasörünü Explorer'da açar. Klasör yoksa oluşturmayı dener —
    /// uygulama ilk kez çalıştırıldığında henüz log yazılmamış olabilir.
    /// </summary>
    public static void OpenLogDirectory(IDialogService dialogService)
    {
        var logDir = App.LogDirectory;

        if (string.IsNullOrEmpty(logDir))
        {
            dialogService.ShowWarning("Log klasör yolu belirlenemedi.");
            return;
        }

        if (!Directory.Exists(logDir))
        {
            try
            {
                Directory.CreateDirectory(logDir);
            }
            catch (Exception ex)
            {
                dialogService.ShowError($"Log klasörü oluşturulamadı: {ex.Message}");
                return;
            }
        }

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", logDir) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            dialogService.ShowError($"Log klasörü açılamadı: {ex.Message}");
        }
    }
}
