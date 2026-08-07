using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Window ve UserControl'ün tema zemini eşdeğerliği.
///
/// NEDEN VAR: Tek kabuk mimarisinde (Faz D) ekranlar Window değil UserControl
/// olacak. Faz B'nin en büyük kırılması, Window stilinin Background'u
/// bağlamamasıydı — 31 pencere koyu temada beyaz zeminde açık metin gösteriyordu.
/// UserControl'e geçerken aynı hatanın ters yönden tekrarlanmaması için iki
/// stilin eşdeğer kaldığı burada sabitlenir.
///
/// Bu testler ekranlar dönüştürülmeden ÖNCE yazıldı: zemin hazır olmadan
/// taşımaya başlamak, hatayı 24 ekrana birden yaymak demekti.
/// </summary>
public class ThemeSurfaceParityTests
{
    /// <summary>Window ve UserControl'ün aynı değeri taşıması gereken özellikler.</summary>
    private static readonly DependencyProperty[] SurfaceProperties =
    [
        Control.BackgroundProperty,
        Control.ForegroundProperty,
        Control.FontFamilyProperty,
        Control.FontSizeProperty,
    ];

    public static TheoryData<string> ThemeNames() => [ThemeTestHost.Light, ThemeTestHost.Dark];

    [Fact]
    public void UserControl_icin_ortuk_tema_stili_tanimli()
    {
        ThemeTestHost.Run(() =>
        {
            var style = WpfApp.Current.TryFindResource(typeof(UserControl)) as Style;

            Assert.True(style is not null,
                "UserControl için örtük stil yok — tek kabuk mimarisinde ekranlar " +
                "tema zeminini kaybeder.");
        });
    }

    /// <summary>
    /// İki stil aynı yüzey özelliklerini vermeli. Biri güncellenip diğeri
    /// unutulursa Window ve UserControl ekranları farklı görünür.
    ///
    /// Karşılaştırma STİL SETTER'LARI üzerinden yapılır, örnek üzerinden değil:
    /// Window bir Border'a çocuk olamadığı için UserControl'le aynı biçimde
    /// gerçeklenemiyor. Setter karşılaştırması zaten doğrudan korunmak istenen
    /// şeydir — iki stilin aynı token'ları vermesi.
    /// </summary>
    [Fact]
    public void Window_ve_UserControl_stilleri_ayni_yuzey_tokenlarini_verir()
    {
        ThemeTestHost.Run(() =>
        {
            var windowStyle      = WpfApp.Current.TryFindResource(typeof(Window)) as Style;
            var userControlStyle = WpfApp.Current.TryFindResource(typeof(UserControl)) as Style;

            Assert.True(windowStyle is not null,      "Window örtük stili yok.");
            Assert.True(userControlStyle is not null, "UserControl örtük stili yok.");

            var failures = new List<string>();

            foreach (var property in SurfaceProperties)
            {
                var fromWindow      = SetterValue(windowStyle!, property);
                var fromUserControl = SetterValue(userControlStyle!, property);

                if (fromWindow is null)
                {
                    failures.Add($"{property.Name}: Window stilinde tanımlı değil");
                    continue;
                }

                if (fromUserControl is null)
                {
                    failures.Add($"{property.Name}: UserControl stilinde tanımlı değil " +
                                 $"(Window'da {fromWindow} var)");
                    continue;
                }

                if (!fromWindow.Equals(fromUserControl, StringComparison.Ordinal))
                    failures.Add($"{property.Name}: Window={fromWindow} UserControl={fromUserControl}");
            }

            Assert.True(failures.Count == 0,
                $"Window/UserControl yüzey paritesi bozuk:{Environment.NewLine}" +
                string.Join(Environment.NewLine, failures));
        });
    }

    /// <summary>
    /// Stildeki setter değerini metne çevirir. DynamicResource kullanan
    /// setter'larda anahtar adı karşılaştırılır — asıl korunmak istenen,
    /// iki stilin AYNI token'a bağlanmış olmasıdır.
    /// </summary>
    private static string? SetterValue(Style style, DependencyProperty property)
    {
        for (var s = style; s is not null; s = s.BasedOn)
        {
            foreach (var setter in s.Setters.OfType<Setter>())
            {
                if (setter.Property != property) continue;

                return setter.Value switch
                {
                    System.Windows.DynamicResourceExtension dyn => $"DynamicResource({dyn.ResourceKey})",
                    null                                        => "<null>",
                    var value                                   => value.ToString(),
                };
            }
        }

        return null;
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void UserControl_kokü_temaya_uygun_zemin_ve_metin_alir(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);
        var theme = ThemeTestHost.LoadTheme(themeName);

        var expectedBackground = ((SolidColorBrush)theme["Theme.AppBackground"]).Color;
        var expectedForeground = ((SolidColorBrush)theme["Theme.Text"]).Color;

        ThemeTestHost.Run(() =>
        {
            var control = Materialize();

            var background = control.Background as SolidColorBrush;
            var foreground = control.Foreground as SolidColorBrush;

            Assert.True(background is not null, $"{themeName}: UserControl zemini yok.");
            Assert.True(foreground is not null, $"{themeName}: UserControl metin rengi yok.");

            Assert.Equal(expectedBackground, background!.Color);
            Assert.Equal(expectedForeground, foreground!.Color);
        });
    }

    /// <summary>
    /// UserControl'ü örtük stilin çözüleceği duruma getirir.
    ///
    /// Bağımsız (ağaca bağlı olmayan) bir elemana örtük stil UYGULANMAZ; bir
    /// kaba alınıp ölçülmesi gerekir. Kabuk mimarisinde ekran zaten bir
    /// ContentControl içinde barınacağı için bu, gerçek kullanımın karşılığıdır.
    /// </summary>
    private static UserControl Materialize()
    {
        var control = new UserControl();
        var host    = new Border { Child = control };

        host.Measure(new Size(400, 300));
        host.Arrange(new Rect(0, 0, 400, 300));
        return control;
    }

    /// <summary>
    /// Zemin ve metin okunur olmalı — parite tek başına yetmez, iki stil de
    /// aynı anda yanlış olabilir.
    /// </summary>
    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void UserControl_zemini_ve_metni_okunur(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);

        ThemeTestHost.Run(() =>
        {
            var control    = Materialize();
            var background = ((SolidColorBrush)control.Background).Color;
            var foreground = ((SolidColorBrush)control.Foreground).Color;
            var ratio      = Contrast.Ratio(foreground, background);

            Assert.True(ratio >= Contrast.AA,
                $"{themeName}: UserControl metni zemininde {ratio:F2}:1 — " +
                $"{Contrast.Describe(foreground)} / {Contrast.Describe(background)}");
        });
    }

    /// <summary>
    /// Tema değişimi UserControl'e de yansımalı. Window için Faz B'de doğrulandı;
    /// UserControl aynı DynamicResource zincirinde olmalı, sabit renge donmamalı.
    /// </summary>
    [Fact]
    public void UserControl_zemini_tema_degisimini_takip_eder()
    {
        ThemeTestHost.ApplyTheme(ThemeTestHost.Light);
        var light = ThemeTestHost.Run(() =>
        {
            return ((SolidColorBrush)Materialize().Background).Color;
        });

        ThemeTestHost.ApplyTheme(ThemeTestHost.Dark);
        var dark = ThemeTestHost.Run(() =>
        {
            return ((SolidColorBrush)Materialize().Background).Color;
        });

        Assert.True(light != dark,
            "UserControl zemini tema değişimini görmüyor — sabit renge donmuş.");
    }

    private static string Describe(object? value) => value switch
    {
        SolidColorBrush brush => Contrast.Describe(brush.Color),
        null                  => "<null>",
        _                     => value.ToString() ?? "<null>",
    };
}
