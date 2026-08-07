using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Shell;

/// <summary>
/// Navigasyon rayındaki bir başlık ve altındaki ekranlar.
///
/// Menü çubuğunun karşılığı: "Yönetim", "Kargo Takip", "Ayarlar" başlıkları
/// menüden birebir geldi. Yetkisi olmayan ekran listeye hiç girmediği için
/// tamamen boşalan bir grup da oluşmaz (bkz. ShellViewModel).
/// </summary>
/// <param name="Title">Grup başlığı.</param>
/// <param name="Screens">Gruptaki ekranlar — kayıt tablosundaki sırayla.</param>
public sealed record ScreenGroup(string Title, IReadOnlyList<ScreenDefinition> Screens);
