using System.Windows;

namespace YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

/// <summary>
/// Parametreli bir ekranın ÜRETİLMİŞ örneği.
///
/// Bazı ekranlar tek başına anlamlı değildir; bir kayıt üzerinde çalışırlar.
/// Kargo Operasyon Merkezi böyledir: seçili bir kargo gönderisi olmadan
/// açılamaz. Bu tür ekranlarda aynı <see cref="ScreenKey"/> altında BİRDEN
/// ÇOK sekme açılabilir — farklı kargolar için ayrı sekmeler.
///
/// Üç bilgiyi birlikte döndürür çünkü üçü de parametreye bağlıdır ve ayrı
/// delegelere bölmek imzayı gereksiz şişirirdi:
///   InstanceKey  sekme kimliği (aynı kayıt ikinci kez açılmasın)
///   Title        sekme başlığı (kayıt numarasını içerebilir)
///   View         ekranın görünümü
/// </summary>
/// <param name="InstanceKey">
/// Aynı ekran türü içinde bu örneği ayırt eden anahtar — genelde kaydın Id'si.
/// Aynı anahtarla ikinci kez açılırsa yeni sekme oluşmaz, mevcut olana odaklanılır.
/// </param>
/// <param name="Title">Sekmede görünen başlık.</param>
/// <param name="View">Ekranın görünümü.</param>
public sealed record ScreenInstance(string InstanceKey, string Title, FrameworkElement View);
