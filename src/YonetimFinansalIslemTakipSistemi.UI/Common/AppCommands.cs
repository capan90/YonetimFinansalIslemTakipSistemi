using System.Windows.Input;

namespace YonetimFinansalIslemTakipSistemi.UI.Common;

/// <summary>
/// Ekranlar arası ortak klavye kısayolları.
///
/// Mevcut Click handler'ları RelayCommand'a çevrilmedi — imzaları ve code-behind
/// bağlantıları korunuyor. Bunun yerine her pencere CommandBinding ile kendi
/// handler'ını çağıran küçük bir sarmalayıcı tanımlar. Böylece kısayol eklemek
/// iş mantığına dokunmaz.
///
/// WPF'in hazır ApplicationCommands.New/Delete/Find yerine kendi komutlarımız:
/// hazır komutlar TextBox gibi kontrollerde kendi davranışlarını devreye sokup
/// beklenmedik sonuç veriyor (ör. Delete metin kutusunda karakter siler).
/// </summary>
public static class AppCommands
{
    public static readonly RoutedUICommand New = new(
        "Yeni Kayıt", nameof(New), typeof(AppCommands),
        [new KeyGesture(Key.N, ModifierKeys.Control)]);

    public static readonly RoutedUICommand Duplicate = new(
        "Kopyala", nameof(Duplicate), typeof(AppCommands),
        [new KeyGesture(Key.D, ModifierKeys.Control)]);

    public static readonly RoutedUICommand DeleteSelected = new(
        "Seçili Kaydı Sil", nameof(DeleteSelected), typeof(AppCommands),
        [new KeyGesture(Key.Delete)]);

    public static readonly RoutedUICommand EditSelected = new(
        "Seçili Kaydı Düzenle", nameof(EditSelected), typeof(AppCommands),
        [new KeyGesture(Key.Enter)]);

    public static readonly RoutedUICommand RefreshList = new(
        "Yenile / Filtrele", nameof(RefreshList), typeof(AppCommands),
        [new KeyGesture(Key.F5)]);

    public static readonly RoutedUICommand FocusSearch = new(
        "Aramaya Odaklan", nameof(FocusSearch), typeof(AppCommands),
        [new KeyGesture(Key.F, ModifierKeys.Control)]);

    public static readonly RoutedUICommand ImportExcel = new(
        "Excel'den İçe Aktar", nameof(ImportExcel), typeof(AppCommands),
        [new KeyGesture(Key.E, ModifierKeys.Control)]);

    public static readonly RoutedUICommand CloseWindow = new(
        "Kapat", nameof(CloseWindow), typeof(AppCommands),
        [new KeyGesture(Key.Escape)]);
}
