using System.IO;

namespace YonetimFinansalIslemTakipSistemi.UI;

/// <summary>
/// ClickOnce dağıtım konumu için tek yapılandırma noktası.
/// Sunucu değişince yalnızca bu dosyayı (ve ClickOnce.pubxml InstallUrl/UpdateUrl'ini) güncelle.
/// Env var YONETIM_UPDATE_PATH tanımlıysa önceliklidir (üretim geçişi için).
/// </summary>
internal static class DeploymentSettings
{
    // Test: \\localhost\YonetimPublish\
    // Üretim: YONETIM_UPDATE_PATH env var ile override edilir
    private static readonly string UncBase =
        Environment.GetEnvironmentVariable("YONETIM_UPDATE_PATH")
        ?? @"\\localhost\YonetimPublish\";

    /// <summary>Yayın klasörünün temel yolu — HealthCheckService ve BackupOptions için.</summary>
    public static string PublishPath => UncBase;

    public static string VersionJsonPath =>
        Path.Combine(UncBase, "version.json");

    // Dosya adı proje adıyla (YonetimFinansalIslemTakipSistemi.UI) eşleşmeli; .application uzantısı eklenir.
    public static string DeploymentFilePath =>
        Path.Combine(UncBase, "YonetimFinansalIslemTakipSistemi.UI.application");
}
