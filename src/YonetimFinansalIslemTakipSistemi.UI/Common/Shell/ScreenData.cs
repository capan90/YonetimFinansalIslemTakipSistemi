using System.Windows;
using System.Windows.Input;

namespace YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

/// <summary>
/// Kabuk ekranlarının veri yaşam döngüsü: ne zaman yüklenir, ne zaman yenilenir.
///
/// NEDEN VAR (Faz E1). Ekranlar pencereyken <c>Loaded</c> ömürde bir kez
/// tetikleniyordu, bu yüzden veriyi oradan çekmek doğruydu. Kabukta öyle
/// değil: WPF <see cref="System.Windows.Controls.TabControl"/> TEK bir
/// ContentPresenter kullanır, seçim değişince giden ekranı görsel ağaçtan
/// söker. Ölçüm (bkz. TabLifecycleTests):
///
///     ilk gösterim   A=(Loaded 1, Unloaded 0)
///     B'ye geçiş     A=(1, 1)              B=(Loaded 1)
///     A'ya dönüş     A=(Loaded 2, 1)       B=(Unloaded 1)
///
/// Yani her sekme geçişi <c>Loaded</c>'ı yeniden tetikliyordu ve veriyi
/// oradan çeken 14 ekranın hepsi her geçişte veritabanına gidiyordu. Bu
/// maliyeti pencere modeli hiç ödemiyordu; kabuk getirdi.
///
/// SÖZLEŞME: <paramref name="load"/> ilk gösterimde bir kez çalışır, sonra
/// yalnızca kullanıcı YENİLE dediğinde (F5 / kabuktaki yenile düğmesi).
/// Otomatik tazeleme yok — veri tazeliği kullanıcının kararı, sekme
/// geçişinin yan etkisi değil.
/// </summary>
public static class ScreenData
{
    /// <summary>
    /// Ekranın yükleme ve yenileme davranışını bağlar.
    /// </summary>
    /// <param name="screen">Kabuk ekranı (UserControl).</param>
    /// <param name="load">
    /// Veriyi çeken iş. İlk gösterimde ve her yenilemede çalışır.
    /// </param>
    /// <param name="initialize">
    /// YALNIZCA ilk gösterimde çalışacak hazırlık — yetkiye bağlı buton
    /// görünürlüğü, filtre kutularının doldurulması, kolon düzeni gibi.
    /// Yenilemede tekrarlanmaz; kullanıcının o sırada yaptığı seçimleri
    /// sıfırlardı.
    /// </param>
    public static void Bind(FrameworkElement screen, Func<Task> load, Func<Task>? initialize = null)
    {
        ArgumentNullException.ThrowIfNull(screen);
        ArgumentNullException.ThrowIfNull(load);

        var loaded = false;

        // Hata yönetimi bilerek eklenmedi: bu kapı 14 ekranın ÖNCEDEN yaptığı
        // işi tek yere taşıyor, davranışını değiştirmiyor. Yükleme hatası
        // eskiden nasıl yüzeye çıkıyorsa öyle çıkmaya devam eder.
        screen.Loaded += async (_, _) =>
        {
            if (loaded) return;
            loaded = true;

            if (initialize is not null) await initialize();
            await load();
        };

        // Ekran kendi F5 bağlamasını zaten kurduysa ikinci kez kurma —
        // komut iki kez çalışır, liste iki kez sorgulanırdı.
        if (!HasRefreshBinding(screen))
            screen.CommandBindings.Add(
                new CommandBinding(AppCommands.RefreshList, async (_, _) => await load()));
    }

    /// <summary>
    /// Yalnızca yenileme bağlar — verisini kendi akışında yükleyen ekranlar
    /// için (ör. ilk gösterimi kendi sırasıyla yöneten panolar).
    /// </summary>
    public static void BindRefresh(FrameworkElement screen, Func<Task> load)
    {
        ArgumentNullException.ThrowIfNull(screen);
        ArgumentNullException.ThrowIfNull(load);

        if (HasRefreshBinding(screen)) return;

        screen.CommandBindings.Add(
            new CommandBinding(AppCommands.RefreshList, async (_, _) => await load()));
    }

    private static bool HasRefreshBinding(FrameworkElement screen) =>
        screen.CommandBindings
              .OfType<CommandBinding>()
              .Any(b => ReferenceEquals(b.Command, AppCommands.RefreshList));
}
