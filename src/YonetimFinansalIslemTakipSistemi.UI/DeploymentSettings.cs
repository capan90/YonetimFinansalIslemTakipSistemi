using System.IO;

namespace YonetimFinansalIslemTakipSistemi.UI;

/// <summary>
/// ClickOnce dağıtım konumu için tek yapılandırma noktası.
/// Öncelik: YONETIM_UPDATE_PATH env var > appsettings "Deployment:UpdatePath" > üretim varsayılanı.
/// Bu değer ClickOnce manifest ProviderURL'i ile aynı olmalıdır (aksi halde manuel
/// "Güncellemeleri Denetle" akışı yanlış sunucuya bakar).
/// </summary>
internal static class DeploymentSettings
{
    // Üretim varsayılanı — ClickOnce ProviderURL ile aynı UNC. Yerel test için
    // env var YONETIM_UPDATE_PATH ya da appsettings.Development.json ile override edilir.
    private const string ProductionDefault = @"\\10.0.0.169\YonetimPublish\";

    // App.xaml.cs Configure çağrısından önce env var / üretim varsayılanı geçerlidir.
    private static string _uncBase = Resolve(null);

    /// <summary>
    /// IConfiguration okunduktan sonra App.xaml.cs tarafından çağrılır.
    /// Böylece güncelleme yolu appsettings üzerinden yönetilebilir (env var yine önceliklidir).
    /// </summary>
    public static void Configure(string? configuredPath) => _uncBase = Resolve(configuredPath);

    private static string Resolve(string? configuredPath)
    {
        var envPath = Environment.GetEnvironmentVariable("YONETIM_UPDATE_PATH");
        var chosen =
            !string.IsNullOrWhiteSpace(envPath)        ? envPath
            : !string.IsNullOrWhiteSpace(configuredPath) ? configuredPath
            : ProductionDefault;

        // Path.Combine'ın doğru çalışması için sonda ters bölü olmalı.
        return chosen.EndsWith("\\") ? chosen : chosen + "\\";
    }

    /// <summary>Yayın klasörünün temel yolu — HealthCheckService ve BackupOptions için.</summary>
    public static string PublishPath => _uncBase;

    public static string VersionJsonPath =>
        Path.Combine(_uncBase, "version.json");

    // Dosya adı proje adıyla (YonetimFinansalIslemTakipSistemi.UI) eşleşmeli; .application uzantısı eklenir.
    public static string DeploymentFilePath =>
        Path.Combine(_uncBase, "YonetimFinansalIslemTakipSistemi.UI.application");
}
