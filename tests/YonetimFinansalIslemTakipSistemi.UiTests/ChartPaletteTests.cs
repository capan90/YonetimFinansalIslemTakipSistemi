using System.Windows;
using System.Windows.Media;
using SkiaSharp;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Grafik paleti kuralları.
///
/// Renkler tema sözlüklerinde (<c>Chart.*</c>) yaşadığı için Faz B'nin parite
/// testi anahtar eşitliğini zaten kapsıyor. Buradaki testler paletin
/// KURALLARINI koruyor: görünürlük eşiği, sıra disiplini, büyüklük skalasının
/// yönü ve "dördüncü renk üretme" yasağı.
/// </summary>
public class ChartPaletteTests
{
    /// <summary>Grafik işaretleri için WCAG non-text eşiği.</summary>
    private const double MarkThreshold = 3.0;

    public static TheoryData<string> ThemeNames() => [ThemeTestHost.Light, ThemeTestHost.Dark];

    /// <summary>Grafiklerin üzerinde durduğu yüzey — kart zemini.</summary>
    private static Color Surface(string themeName)
    {
        var theme = ThemeTestHost.LoadTheme(themeName);
        return ((SolidColorBrush)theme["Theme.Surface"]).Color;
    }

    private static Color Wpf(SKColor c) => Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue);

    // ── Görünürlük ───────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Kategorik_renkler_yuzeyde_gorunur(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);
        var surface = Surface(themeName);

        ThemeTestHost.Run(() =>
        {
            for (var i = 0; i < ChartPalette.CategoricalCount; i++)
            {
                var color = Wpf(ChartPalette.Categorical(i));
                var ratio = Contrast.Ratio(color, surface);

                Assert.True(ratio >= MarkThreshold,
                    $"{themeName}: {i + 1}. kategorik renk {Contrast.Describe(color)} yüzeyde " +
                    $"{ratio:F2}:1 — grafik işareti için {MarkThreshold:F1}:1 gerekir.");
            }
        });
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Yon_renkleri_yuzeyde_gorunur(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);
        var surface = Surface(themeName);

        ThemeTestHost.Run(() =>
        {
            foreach (var (name, color) in new[]
                     {
                         ("Inflow",  Wpf(ChartPalette.Inflow())),
                         ("Outflow", Wpf(ChartPalette.Outflow())),
                         ("Sparkline", Wpf(ChartPalette.Sparkline())),
                     })
            {
                var ratio = Contrast.Ratio(color, surface);
                Assert.True(ratio >= MarkThreshold,
                    $"{themeName}: {name} {Contrast.Describe(color)} yüzeyde {ratio:F2}:1");
            }
        });
    }

    /// <summary>
    /// Büyüklük skalasının açık ucu yüzeyle düşük kontrastlıdır — bu bir hata
    /// değil, skalanın doğasıdır. Görünürlüğü kenarlık taşır; o yüzden
    /// kenarlığın eşiği tutması ZORUNLUDUR.
    /// </summary>
    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Buyukluk_skalasinin_kenarligi_gorunur(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);
        var surface = Surface(themeName);

        ThemeTestHost.Run(() =>
        {
            var stroke = Wpf(ChartPalette.SequentialStroke());
            var ratio  = Contrast.Ratio(stroke, surface);

            Assert.True(ratio >= MarkThreshold,
                $"{themeName}: skala kenarlığı {Contrast.Describe(stroke)} yüzeyde {ratio:F2}:1 — " +
                "açık uçtaki dolgular bu kenarlık olmadan kaybolur.");
        });
    }

    // ── Sıra disiplini ───────────────────────────────────────────────────────

    /// <summary>
    /// Kategorik renkler HUE ile ayrışır: mavi → turuncu → yeşil.
    ///
    /// Parlaklıkla ayrışmazlar (birbirlerine karşı ~1.1–1.4:1) — bu kategorik
    /// paletlerin bilinen sınırıdır ve GRİ BASKIDA ayrışmazlar. Bu yüzden
    /// iki ve daha fazla seride legend zorunludur; renk tek anlam taşıyıcı
    /// olamaz. Test hue baskınlığını doğrular, sahte bir parlaklık eşiği aramaz.
    /// </summary>
    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Kategorik_sira_mavi_turuncu_yesil(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);

        ThemeTestHost.Run(() =>
        {
            var c0 = ChartPalette.Categorical(0);
            var c1 = ChartPalette.Categorical(1);
            var c2 = ChartPalette.Categorical(2);

            Assert.True(c0.Blue  > c0.Red && c0.Blue  > c0.Green,
                $"{themeName}: 1. seri mavi baskın olmalı, gelen {Contrast.Describe(Wpf(c0))}");
            Assert.True(c1.Red   > c1.Green && c1.Red > c1.Blue,
                $"{themeName}: 2. seri turuncu/kırmızı baskın olmalı, gelen {Contrast.Describe(Wpf(c1))}");
            Assert.True(c2.Green > c2.Red && c2.Green > c2.Blue,
                $"{themeName}: 3. seri yeşil baskın olmalı, gelen {Contrast.Describe(Wpf(c2))}");
        });
    }

    [Fact]
    public void Dorduncu_kategorik_renk_yoktur()
    {
        ThemeTestHost.Run(() =>
        {
            // Sessizce döngüye girmek iki farklı kategoriyi aynı renkle göstermek demektir.
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => ChartPalette.Categorical(ChartPalette.CategoricalCount));

            Assert.Contains("GroupSmall", ex.Message, StringComparison.Ordinal);
        });
    }

    /// <summary>
    /// Yön verisinde kırmızı/yeşil kullanılmaz — renk körlüğünün en zayıf
    /// eksenidir. Mavi–turuncu çifti beklenir.
    /// </summary>
    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Yon_verisinde_kirmizi_yesil_kullanilmaz(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);

        ThemeTestHost.Run(() =>
        {
            var inflow  = ChartPalette.Inflow();
            var outflow = ChartPalette.Outflow();

            Assert.True(inflow.Blue > inflow.Red && inflow.Blue > inflow.Green,
                $"{themeName}: giriş yönü mavi olmalı, gelen {Contrast.Describe(Wpf(inflow))}");
            Assert.True(outflow.Red > outflow.Green && outflow.Red > outflow.Blue,
                $"{themeName}: çıkış yönü turuncu olmalı, gelen {Contrast.Describe(Wpf(outflow))}");

            // Yeşil–kırmızı çifti olmadığını açıkça reddet
            var outflowIsGreen = outflow.Green > outflow.Red && outflow.Green > outflow.Blue;
            var inflowIsGreen  = inflow.Green  > inflow.Red  && inflow.Green  > inflow.Blue;
            Assert.False(outflowIsGreen || inflowIsGreen,
                $"{themeName}: yön verisinde yeşil kullanılmış — kırmızı/yeşil ekseni yasak.");
        });
    }

    // ── Büyüklük skalası ─────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Buyukluk_skalasi_tek_yonde_ilerler(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);

        ThemeTestHost.Run(() =>
        {
            var steps = Enumerable.Range(0, ChartPalette.SequentialSteps)
                                  .Select(i => Wpf(ChartPalette.Sequential(i, ChartPalette.SequentialSteps)))
                                  .ToList();

            // Yön temaya göre değişir: açık temada koyulaşır, koyu temada açılır.
            var ascending = Contrast.Ratio(steps[^1], Colors.Black) > Contrast.Ratio(steps[0], Colors.Black);

            for (var i = 1; i < steps.Count; i++)
            {
                var prev = Contrast.Ratio(steps[i - 1], Colors.Black);
                var cur  = Contrast.Ratio(steps[i],     Colors.Black);

                Assert.True(ascending ? cur > prev : cur < prev,
                    $"{themeName}: skala {i}. adımda yön değiştiriyor — büyüklük sırası okunmaz. " +
                    $"{Contrast.Describe(steps[i - 1])} → {Contrast.Describe(steps[i])}");
            }
        });
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void Buyukluk_skalasi_kategorik_paletten_farklidir(string themeName)
    {
        ThemeTestHost.ApplyTheme(themeName);

        ThemeTestHost.Run(() =>
        {
            // Tek boyutlu büyüklük verisinde renk KİMLİK taşımaz; skalanın
            // adımları tek hue olmalı, kategorik üçlüyle karışmamalı.
            var seq = Enumerable.Range(0, ChartPalette.SequentialSteps)
                                .Select(i => ChartPalette.Sequential(i, ChartPalette.SequentialSteps))
                                .ToList();

            Assert.All(seq, c => Assert.True(c.Blue >= c.Red,
                $"{themeName}: büyüklük skalası tek hue (mavi) olmalı, gelen {Contrast.Describe(Wpf(c))}"));
        });
    }

    // ── Tema duyarlılığı ─────────────────────────────────────────────────────

    /// <summary>
    /// Paletin varlık sebebi: LiveCharts DynamicResource görmez. Renk ÇİZİM
    /// ANINDA okunmalı ki tema değişince yeni değer gelsin.
    /// </summary>
    [Fact]
    public void Palet_aktif_temayi_okur()
    {
        ThemeTestHost.ApplyTheme(ThemeTestHost.Light);
        var light = ThemeTestHost.Run(() => ChartPalette.Categorical(0));

        ThemeTestHost.ApplyTheme(ThemeTestHost.Dark);
        var dark = ThemeTestHost.Run(() => ChartPalette.Categorical(0));

        Assert.True(light != dark,
            "Palet tema değişimini görmüyor — grafikler eski renkte donar.");
    }

    // ── "Diğer" gruplama ─────────────────────────────────────────────────────

    [Fact]
    public void Fazla_kategori_Diger_altinda_toplanir()
    {
        var result = ChartPalette.GroupSmall(
        [
            ("Aras",   50),
            ("Yurtiçi", 30),
            ("MNG",    20),
            ("Sürat",   8),
            ("PTT",     5),
            ("UPS",     2),
        ]);

        // 3 kategorik renk + 1 "Diğer"
        Assert.Equal(4, result.Count);
        Assert.Equal(["Aras", "Yurtiçi", "MNG", "Diğer"], result.Select(r => r.Label));
        Assert.Equal(15, result[^1].Value);   // 8 + 5 + 2
    }

    [Fact]
    public void Toplam_deger_gruplamada_korunur()
    {
        var items = new (string, double)[] { ("a", 10), ("b", 9), ("c", 8), ("d", 7), ("e", 6) };
        var result = ChartPalette.GroupSmall(items);

        Assert.Equal(items.Sum(i => i.Item2), result.Sum(r => r.Value));
    }

    [Fact]
    public void Tek_kalan_kategori_Diger_yapilmaz()
    {
        // Tek bir kalemi "Diğer" diye göstermek bilgi kaybıdır; adıyla kalır.
        var result = ChartPalette.GroupSmall([("a", 10), ("b", 8), ("c", 6), ("d", 4)]);

        Assert.Equal(4, result.Count);
        Assert.DoesNotContain("Diğer", result.Select(r => r.Label));
        Assert.Equal("d", result[^1].Label);
    }

    [Fact]
    public void Palet_kapasitesini_asmayan_liste_degismez()
    {
        var result = ChartPalette.GroupSmall([("a", 5), ("b", 3)]);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain("Diğer", result.Select(r => r.Label));
    }

    [Fact]
    public void Gruplama_buyukten_kucuge_siralar()
    {
        var result = ChartPalette.GroupSmall([("kucuk", 1), ("buyuk", 100), ("orta", 50)]);

        Assert.Equal(["buyuk", "orta", "kucuk"], result.Select(r => r.Label));
    }

    /// <summary>
    /// Gruplama sonucu her zaman kategorik palete sığmalı — aksi hâlde
    /// <see cref="ChartPalette.Categorical"/> hata verir ve grafik çizilmez.
    /// </summary>
    [Fact]
    public void Gruplama_sonucu_her_zaman_palete_sigar()
    {
        foreach (var count in Enumerable.Range(1, 12))
        {
            var items  = Enumerable.Range(0, count).Select(i => ($"k{i}", (double)(count - i)));
            var result = ChartPalette.GroupSmall(items);

            Assert.True(result.Count <= ChartPalette.CategoricalCount + 1,
                $"{count} kategori → {result.Count} sonuç; palet kapasitesi aşıldı.");
        }
    }
}
