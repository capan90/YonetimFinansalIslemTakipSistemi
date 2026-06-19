namespace YonetimFinansalIslemTakipSistemi.UI.Abstractions;

public interface IUpdateService
{
    /// <summary>
    /// ClickOnce launcher aracılığıyla başlatıldığını döndürür.
    /// false → debug veya doğrudan exe; manuel kontrol devre dışı kalır.
    /// </summary>
    bool IsClickOnceDeployment { get; }

    /// <summary>
    /// UNC klasöründeki version.json okunur; mevcut Assembly sürümüyle karşılaştırılır.
    /// Dosya okunamazsa veya ağ erişimi yoksa exception fırlatmaz; ErrorMessage döner.
    /// </summary>
    Task<UpdateCheckResult> CheckForUpdateAsync();

    /// <summary>
    /// .application deployment dosyasını shell ile açar; ClickOnce güncelleme sürecini başlatır.
    /// </summary>
    void LaunchInstaller();
}

public record UpdateCheckResult(
    bool     IsUpdateAvailable,
    Version? LatestVersion,
    Version? CurrentVersion,
    string?  ErrorMessage);
