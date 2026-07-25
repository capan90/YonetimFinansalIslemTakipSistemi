namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// Bir yükleme/yenileme işini seri hale getirir: işlem sürerken gelen ek istekler
/// tek bir "bekleyen" isteğe indirgenir ve mevcut işlem bitince YALNIZCA bir kez daha
/// çalışır (aradaki tüm istekler atlanır). Böylece:
///   1) Oturum boyunca paylaşılan tek DbContext üzerinde eşzamanlı sorgu çalışmaz
///      ("A second operation was started on this context instance" hatasının önlenmesi),
///   2) Hızlı ardışık tetiklemeler (filtre/seçim değişimi) sonsuz/gereksiz yükleme
///      döngüsü oluşturmaz.
///
/// İşlem delegesi çalıştığı ANDAKİ durumu (ör. seçili filtre) okuduğundan, kuyruğa alınan
/// tekrar çalıştırma her zaman EN SON durumu uygular. Parametreli senaryolarda çağıran,
/// en son argümanları bir alanda tutup delege içinde okur (bkz. CargoShipmentEditViewModel).
///
/// Tek iş parçacıklı (WPF Dispatcher) kullanım için tasarlanmıştır; bu yüzden kilit yoktur.
/// WPF'ye bağlı değildir → Application katmanında yaşar ve net9.0 test projesinden test edilir.
/// </summary>
public sealed class ReloadCoordinator
{
    private bool _running;
    private bool _pending;

    /// <summary>Şu anda bir işlem yürüyor mu (test/teşhis görünürlüğü).</summary>
    public bool IsRunning => _running;

    /// <summary>
    /// İşlemi çalıştırır. Zaten bir işlem sürüyorsa, bu çağrı yalnızca "bekleyen" bayrağını
    /// kaldırır ve hemen döner; işlem, mevcut çalışma bitince bir kez daha koşar.
    /// </summary>
    public async Task RunAsync(Func<Task> operation)
    {
        if (operation is null) throw new ArgumentNullException(nameof(operation));

        // Zaten çalışıyorsa: yalnızca en son isteği işaretle, çift sorgu başlatma.
        if (_running)
        {
            _pending = true;
            return;
        }

        _running = true;
        try
        {
            do
            {
                _pending = false;
                await operation();
            }
            // İşlem sürerken yeni istek geldiyse son durumu uygulamak için bir kez daha koş.
            while (_pending);
        }
        finally
        {
            // Hata durumunda da kilit açılır; bekleyen istek düşer (bir sonraki kullanıcı
            // eylemi yeniden tetikler) — sürekli hata veren işlemde sonsuz döngü olmaz.
            _running = false;
        }
    }
}
