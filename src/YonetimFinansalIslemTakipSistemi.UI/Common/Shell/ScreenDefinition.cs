using System.Windows;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

/// <summary>
/// Bir kabuk ekranının tanımı: kimliği, başlığı, gerektirdiği yetkiler ve
/// görünümünü nasıl üreteceği.
///
/// TASARIM NOTU — neden fabrika delegesi:
/// Kabuk ekranları kendisi "new"lemez; her ekranın kurucusu farklı bağımlılık
/// alıyor. Ekran üretimi bu delegeye devredilir ve delege ekranın kendi
/// dosyasında, kendi bağımlılıklarını bilerek yazılır. Kabuk yalnızca
/// <see cref="IServiceProvider"/>'ı geçirir — projede zaten kullanılan desen
/// (her pencere kurucusu IServiceProvider alıyor). Ayrı bir fabrika arayüzü
/// katmanı veya yansımaya dayalı çözüm kurulmadı; bu ölçek için gereksiz.
///
/// <see cref="CreateView"/> null ise ekran HENÜZ TAŞINMAMIŞ demektir. Kabuk
/// onu navigasyonda göstermez ve açmaya çalışırsa reddeder. Böylece kayıt
/// tablosu ekranların tamamını baştan tarif edebilir ama taşıma kademeli
/// ilerler; sahte/boş görünüm üretmeye gerek kalmaz.
/// </summary>
/// <param name="Key">Benzersiz ekran kimliği.</param>
/// <param name="Title">Sekmede ve navigasyon rayında görünen kullanıcı dostu başlık.</param>
/// <param name="RequiredPermissions">
/// Ekranı görmek için gereken yetkiler — HERHANGİ BİRİ yeterlidir (VEYA).
/// Boş liste "yetki aranmaz" demektir.
///
/// Neden liste: mevcut ekranların birçoğu tek bir yetkiyle korunmuyor.
/// Örneğin Gelen Kargolar <c>CanViewIncomingCargo</c> VEYA
/// <c>CanManageIncomingCargo</c> ile açılıyor. Tekil alan bu gerçeği
/// taşıyamıyordu ve kabuk mevcut davranıştan sapardı.
/// </param>
/// <param name="CreateView">
/// TEKİL ekranın görünümünü üretir — kabukta bir kez açılır.
/// Parametreli ekranlarda <c>null</c> bırakılır.
/// </param>
/// <param name="CreateInstance">
/// PARAMETRELİ ekranın örneğini üretir. Aynı ekran türü altında farklı
/// kayıtlar için ayrı sekmeler açılabilir (bkz. <see cref="ScreenInstance"/>).
/// Tekil ekranlarda <c>null</c> bırakılır.
/// </param>
/// <param name="CanClose">
/// Sekme KULLANICI tarafından kapatılabilir mi. Logout sırasında bu bayrak
/// dikkate alınmaz; kabuk her hâlükârda boşalır.
/// </param>
/// <param name="IsParameterized">
/// Ekran bir KAYIT üzerinde mi çalışıyor.
///
/// Ekranın doğasıdır ve taşınmadan ÖNCE de bilinir; bu yüzden fabrikadan
/// türetilmez, açıkça bildirilir. Türetilseydi "henüz taşınmamış parametreli
/// ekran" ifade edilemezdi — kayıt tablosu bugün tam olarak o durumda.
///
/// Parametreli ekran parametresiz açılamaz, tekil ekran da parametreyle
/// açılamaz; ikisi de çağıran tarafındaki hatayı gösterir.
/// </param>
/// <param name="NavGroup">
/// Navigasyon rayındaki başlık grubu.
///
/// Gruplar MainWindow'un menü çubuğundan birebir alındı (Yönetim / Kargo Takip
/// / Ayarlar): kullanıcının bildiği sıralama korunsun diye. Boş bırakılırsa
/// öğe gruplandırılmadan en üstte durur.
/// </param>
public sealed record ScreenDefinition(
    ScreenKey                                         Key,
    string                                            Title,
    IReadOnlyList<PermissionType>                     RequiredPermissions,
    Func<IServiceProvider, FrameworkElement>?         CreateView      = null,
    Func<IServiceProvider, object, ScreenInstance>?   CreateInstance  = null,
    bool                                              IsParameterized = false,
    bool                                              CanClose        = true,
    string                                            NavGroup        = "")
{
    /// <summary>
    /// Ekran UserControl'e taşındı ve kabukta açılabilir mi.
    /// Doğasına uyan fabrikanın dolu olması gerekir: tekil ekran
    /// <see cref="CreateView"/>, parametreli ekran <see cref="CreateInstance"/>.
    /// </summary>
    public bool IsMigrated => IsParameterized
        ? CreateInstance is not null
        : CreateView is not null;

    /// <summary>
    /// Kullanıcı bu ekranı görebilir mi. Yetki listesi boşsa herkese açıktır;
    /// doluysa HERHANGİ BİRİNİN olması yeterlidir.
    /// </summary>
    public bool IsAllowedFor(Func<PermissionType, bool> hasPermission)
        => RequiredPermissions.Count == 0 || RequiredPermissions.Any(hasPermission);
}
