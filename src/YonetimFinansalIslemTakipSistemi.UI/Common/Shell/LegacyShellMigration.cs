namespace YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

/// <summary>
/// LEGACY - SHELL MIGRATION  (Faz E3 — "Legacy Freeze")
///
/// Faz D tüm ekranları UserControl'e taşıdı ve kabuk tek başlangıç penceresi
/// oldu. Geriye iki şey kaldı:
///
///   • <c>MainWindow</c> — menü çubuklu eski ana pencere. App artık doğrudan
///     ShellWindow açıyor; bu sınıf hiçbir yerden ÖRNEKLENMİYOR.
///   • 15 İNCE BARINDIRICI pencere — içerikleri ilgili *Screen kontrolünde;
///     yalnızca başlık/boyut/ikon taşıyorlar. Onları da yalnızca MainWindow
///     ve ekranlardaki "kabuk yoksa pencere aç" yedek dalları açıyordu.
///
/// NEDEN SİLİNMEDİ. Kabuk henüz gerçek kullanımda birkaç gün geçirmedi.
/// Silmek geri dönüşü pahalı bir karar; dondurmak değil. Bu yüzden bu sprint
/// yalnızca DONDURUR:
///
///   • yeni kod bu sınıflara YAZILMAZ,
///   • yeni özellikler yalnızca kabuk mimarisine eklenir,
///   • testler yalnızca kabuk üzerinden çalışır,
///   • kullanan her yer <c>[Obsolete]</c> uyarısıyla görünür kalır.
///
/// KALDIRMA (sonraki sprint — "Legacy Removal"): MainWindow, 15 ince
/// barındırıcı, ekranlardaki Navigator yedek dalları ve yalnızca bu yol için
/// duran servisler tek seferde silinecek. Envanter:
/// <c>docs/02-Architecture/Legacy-Shell-Migration.md</c>.
/// </summary>
public static class LegacyShellMigration
{
    /// <summary>
    /// <c>[Obsolete]</c> gerekçesi. Tek metin: kaldırma kararı değişirse tek
    /// yerde değişir, 16 dosyada değil.
    /// </summary>
    public const string Reason =
        "Legacy - Shell Migration: bu pencere donduruldu, yerini kabuk sekmesi aldı. " +
        "Yeni kod eklemeyin. Sonraki sprintte (Legacy Removal) kaldırılacak — " +
        "bkz. docs/02-Architecture/Legacy-Shell-Migration.md";
}
