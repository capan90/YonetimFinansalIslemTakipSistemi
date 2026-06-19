using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;

namespace YonetimFinansalIslemTakipSistemi.UI.Services;

/// <summary>
/// UNC ağ klasörüne dayalı güncelleme kontrolü.
/// Startup kontrolü ClickOnce Foreground tarafından (sıfır kod) yönetilir.
/// Bu sınıf yalnızca manuel "Güncellemeleri Denetle" akışından sorumludur.
/// </summary>
public class UpdateService : IUpdateService
{
    // UNC konumu DeploymentSettings üzerinden tek noktadan yönetilir.
    // Sunucu değişince yalnızca DeploymentSettings.cs ve ClickOnce.pubxml güncellenir.
    private static string VersionJsonPath    => DeploymentSettings.VersionJsonPath;
    private static string DeploymentFilePath => DeploymentSettings.DeploymentFilePath;

    // ClickOnce uygulamayı %LOCALAPPDATA%\Apps\ altına kurar.
    // System.Deployment.Application .NET 9'da mevcut değildir; konum kontrolü güvenilir alternatiftir.
    private static readonly string ClickOnceInstallBase =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Apps");

    public bool IsClickOnceDeployment =>
        AppContext.BaseDirectory.StartsWith(ClickOnceInstallBase, StringComparison.OrdinalIgnoreCase);

    public async Task<UpdateCheckResult> CheckForUpdateAsync()
    {
        var current = CurrentVersion();

        if (!IsClickOnceDeployment)
            return new UpdateCheckResult(false, null, current, null);

        try
        {
            // UNC erişimi: HttpClient değil, dosya okuma
            var json   = await File.ReadAllTextAsync(VersionJsonPath);
            var doc    = JsonDocument.Parse(json);
            var raw    = doc.RootElement.GetProperty("version").GetString() ?? string.Empty;
            var latest = Version.Parse(raw);

            return new UpdateCheckResult(latest > current, latest, current, null);
        }
        catch (IOException)
        {
            return new UpdateCheckResult(false, null, current, "io_error");
        }
        catch (Exception)
        {
            return new UpdateCheckResult(false, null, current, "unknown_error");
        }
    }

    public void LaunchInstaller()
        => Process.Start(new ProcessStartInfo(DeploymentFilePath) { UseShellExecute = true });

    private static Version CurrentVersion()
        => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
}
