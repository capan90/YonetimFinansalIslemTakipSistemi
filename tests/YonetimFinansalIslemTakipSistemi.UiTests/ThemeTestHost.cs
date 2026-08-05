using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Testlerin ortak WPF barındırıcısı.
///
/// WPF nesneleri thread'e bağlıdır ve <see cref="WpfApp.Current"/> olmadan
/// DynamicResource aramaları çözülmez. Bu sınıf tek bir kalıcı STA thread açar,
/// üzerinde bir WPF Application örneği kurar ve uygulamanın gerçek
/// kaynak sözlüklerini (AppStyles + Controls + PrintStyles + Icons + tema)
/// yükler. Testler işi <see cref="Run{T}"/> ile o thread'e devreder.
///
/// Böylece "uygulamayı gerçekten çalıştırmadan" ama gerçek kaynak zinciriyle
/// doğrulama yapılır. Büyük bir UI otomasyon framework'ü gerekmez.
/// </summary>
public static class ThemeTestHost
{
    private const string UiAssembly = "YonetimFinansalIslemTakipSistemi.UI";

    private static readonly object Gate = new();
    private static Dispatcher? _dispatcher;

    /// <summary>Tema sözlüğü adları — testlerde parametre olarak kullanılır.</summary>
    public const string Light = "LightTheme";
    public const string Dark  = "DarkTheme";

    private static Dispatcher Dispatcher
    {
        get
        {
            lock (Gate)
            {
                if (_dispatcher is not null) return _dispatcher;

                var ready  = new ManualResetEventSlim(false);
                var thread = new Thread(() =>
                {
                    // WpfApp.Current yalnızca bir kez kurulabilir; bu thread
                    // test süreci boyunca yaşar.
                    if (WpfApp.Current is null)
                    {
                        var app = new WpfApp { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                        app.Resources = BuildApplicationResources(Light);
                    }

                    _dispatcher = Dispatcher.CurrentDispatcher;
                    ready.Set();
                    Dispatcher.Run();
                })
                {
                    IsBackground = true,
                    Name         = "WpfThemeTestHost"
                };

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                ready.Wait(TimeSpan.FromSeconds(30));

                return _dispatcher ?? throw new InvalidOperationException("WPF test thread başlatılamadı.");
            }
        }
    }

    /// <summary>İşi STA thread'inde çalıştırır ve sonucu döner.</summary>
    public static T Run<T>(Func<T> work) => Dispatcher.Invoke(work);

    /// <summary>İşi STA thread'inde çalıştırır.</summary>
    public static void Run(Action work) => Dispatcher.Invoke(work);

    /// <summary>Aktif temayı değiştirir (uygulamadaki ThemeService ile aynı desen).</summary>
    public static void ApplyTheme(string themeName) => Run(() =>
    {
        var dicts = WpfApp.Current.Resources.MergedDictionaries;
        var old = dicts.FirstOrDefault(d =>
            d.Source is not null &&
            (d.Source.OriginalString.Contains(Light) || d.Source.OriginalString.Contains(Dark)));

        if (old is not null) dicts.Remove(old);
        dicts.Add(LoadDictionary($"Resources/Themes/{themeName}.xaml"));
    });

    /// <summary>Tek bir tema sözlüğünü bağımsız olarak yükler (parite/kontrast testleri).</summary>
    public static ResourceDictionary LoadTheme(string themeName) =>
        Run(() => LoadDictionary($"Resources/Themes/{themeName}.xaml"));

    /// <summary>Aktif kaynak zincirinden bir fırça çözer.</summary>
    public static SolidColorBrush ResolveBrush(string key) => Run(() =>
    {
        var value = WpfApp.Current.TryFindResource(key);
        Assert.True(value is not null, $"'{key}' kaynağı çözülemedi.");
        Assert.True(value is SolidColorBrush, $"'{key}' bir SolidColorBrush değil: {value!.GetType().Name}");
        return (SolidColorBrush)value;
    });

    /// <summary>Kaynağın çözülüp çözülmediğini söyler; testte tek tek assert etmeden tarama için.</summary>
    public static bool CanResolve(string key) =>
        Run(() => WpfApp.Current.TryFindResource(key) is not null);

    private static ResourceDictionary BuildApplicationResources(string themeName)
    {
        var root = new ResourceDictionary();
        // App.xaml ile AYNI sıra — sıra önemlidir (Controls, AppStyles'taki
        // ortak kaynaklara DynamicResource ile başvurur).
        root.MergedDictionaries.Add(LoadDictionary("Resources/AppStyles.xaml"));
        root.MergedDictionaries.Add(LoadDictionary("Resources/Controls.xaml"));
        root.MergedDictionaries.Add(LoadDictionary("Resources/PrintStyles.xaml"));
        root.MergedDictionaries.Add(LoadDictionary("Resources/Icons.xaml"));
        root.MergedDictionaries.Add(LoadDictionary($"Resources/Themes/{themeName}.xaml"));
        return root;
    }

    private static ResourceDictionary LoadDictionary(string relativePath) =>
        new() { Source = new Uri($"pack://application:,,,/{UiAssembly};component/{relativePath}", UriKind.Absolute) };
}
