using System.Windows.Media;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace YonetimFinansalIslemTakipSistemi.UI.Common;

/// <summary>
/// Grafik renklerinin TEK okuma noktası.
///
/// NEDEN VAR: LiveCharts SkiaSharp ile çizer, WPF fırçalarını kullanmaz ve
/// <c>DynamicResource</c>'u GÖRMEZ. Faz B'de converter'larda çözülen sorunun
/// aynısı burada daha sert biçimde geçerlidir: bir kez boyanan seri, tema
/// değiştiğinde eski renginde kalır.
///
/// Çözüm: renkler burada, ÇİZİM ANINDA tema sözlüğünden okunur; tema
/// değiştiğinde <see cref="ThemeChanged"/> tetiklenir ve pencereler serilerini
/// yeniden kurar. Grafik kodunda hiçbir yerde hex yazılmaz.
///
/// PALET KURALLARI (bkz. docs/02-Architecture/ChartPalette.md):
///   • Kategorik seriler SIRAYLA: mavi → turuncu → yeşil. Döngü yok, 4. renk yok.
///   • Üçten fazla kategori → renk eklenmez, küçükler "Diğer" altında toplanır
///     (<see cref="GroupSmall"/>).
///   • Tek boyutlu büyüklük verisi → kategorik palet DEĞİL, tek hue açık→koyu.
///   • Yön verisi (borç/alacak) → kırmızı/yeşil DEĞİL, mavi–turuncu.
/// </summary>
public static class ChartPalette
{
    /// <summary>Kategorik palette tanımlı seri sayısı. Dördüncü renk bilinçli olarak yoktur.</summary>
    public const int CategoricalCount = 3;

    /// <summary>Tek hue büyüklük skalasındaki adım sayısı.</summary>
    public const int SequentialSteps = 5;

    /// <summary>
    /// Tema değiştiğinde tetiklenir. Grafik barındıran pencereler buna abone olup
    /// serilerini yeniden kurar — SkiaSharp çizimi kendiliğinden güncellenmez.
    /// Aboneler pencere kapanırken çözülmelidir (bkz. Unloaded).
    /// </summary>
    public static event Action? ThemeChanged;

    /// <summary>Tema uygulandıktan SONRA çağrılır (ThemeService).</summary>
    public static void NotifyThemeChanged() => ThemeChanged?.Invoke();

    // ── Veri renkleri ────────────────────────────────────────────────────────

    /// <summary>
    /// Kategorik seri rengi. <paramref name="index"/> 0..2 arasındadır.
    /// Dışına çıkılırsa hata verir — sessizce döngüye girmek, iki farklı
    /// kategoriyi aynı renkle göstermek demektir.
    /// </summary>
    public static SKColor Categorical(int index)
    {
        if (index is < 0 or >= CategoricalCount)
            throw new ArgumentOutOfRangeException(nameof(index),
                $"Kategorik palette {CategoricalCount} renk var; {index}. renk istendi. " +
                "Renk eklemek yerine kategorileri GroupSmall ile azaltın veya grafiği bölün.");

        return Color($"Chart.Series{index + 1}");
    }

    /// <summary>
    /// Büyüklük skalasından renk. <paramref name="rank"/> 0 = en küçük.
    /// Kategori sayısı adım sayısından farklıysa oransal eşlenir.
    /// </summary>
    public static SKColor Sequential(int rank, int total)
    {
        if (total <= 0) return Color("Chart.Sequential.3");

        var step = total == 1
            ? SequentialSteps - 1
            : (int)Math.Round((double)rank / (total - 1) * (SequentialSteps - 1));

        step = Math.Clamp(step, 0, SequentialSteps - 1);
        return Color($"Chart.Sequential.{step + 1}");
    }

    /// <summary>
    /// Büyüklük barlarının kenarlığı. Skalanın açık ucu yüzeyle düşük kontrastlıdır;
    /// görünürlüğü dolgu değil bu kenarlık taşır.
    /// </summary>
    public static SKColor SequentialStroke() => Color("Chart.Sequential.Stroke");

    /// <summary>Giriş/alacak yönü — mavi.</summary>
    public static SKColor Inflow() => Color("Chart.Direction.Inflow");

    /// <summary>Çıkış/borç yönü — turuncu.</summary>
    public static SKColor Outflow() => Color("Chart.Direction.Outflow");

    /// <summary>Bakiye kartı sparkline'ı.</summary>
    public static SKColor Sparkline() => Color("Chart.Sparkline");

    // ── Grafik kromu ─────────────────────────────────────────────────────────
    // Ayrı gri seti tanımlanmadı; mevcut tema rolleri kullanılır.

    public static SKColor AxisText()  => Color("Theme.LabelText");
    public static SKColor GridLine()  => Color("Theme.Border");
    public static SKColor LegendText() => Color("Theme.Text");

    // ── Boya yardımcıları ────────────────────────────────────────────────────

    public static SolidColorPaint Fill(SKColor color) => new(color);

    public static SolidColorPaint Stroke(SKColor color, float thickness = 2f)
        => new(color) { StrokeThickness = thickness };

    /// <summary>Alan dolgusu — çizgi grafiğinde seri altındaki yumuşak dolgu.</summary>
    public static SolidColorPaint AreaFill(SKColor color, byte alpha = 38)
        => new(color.WithAlpha(alpha));

    // ── "Diğer" gruplama ─────────────────────────────────────────────────────

    /// <summary>
    /// Kategori sayısını palet kapasitesine indirir: en büyük
    /// <paramref name="keep"/> tanesi korunur, geri kalanlar tek bir "Diğer"
    /// dilimi altında toplanır.
    ///
    /// Saf fonksiyondur — dördüncü renk üretmek yerine veriyi azaltma kuralının
    /// çalıştırılabilir hâlidir.
    /// </summary>
    /// <param name="items">Etiket + değer çiftleri.</param>
    /// <param name="keep">Korunacak kategori sayısı (varsayılan: palet kapasitesi).</param>
    /// <param name="otherLabel">Toplama dilimi etiketi.</param>
    public static IReadOnlyList<(string Label, double Value)> GroupSmall(
        IEnumerable<(string Label, double Value)> items,
        int keep = CategoricalCount,
        string otherLabel = "Diğer")
    {
        ArgumentNullException.ThrowIfNull(items);
        if (keep < 1) throw new ArgumentOutOfRangeException(nameof(keep));

        var ordered = items.OrderByDescending(i => i.Value).ToList();
        if (ordered.Count <= keep) return ordered;

        var kept  = ordered.Take(keep).ToList();
        var rest  = ordered.Skip(keep).ToList();

        // Tek bir kalem kaldıysa "Diğer" demek bilgi kaybıdır; adıyla bırakılır.
        if (rest.Count == 1)
        {
            kept.Add(rest[0]);
            return kept;
        }

        kept.Add((otherLabel, rest.Sum(r => r.Value)));
        return kept;
    }

    // ── Token okuma ──────────────────────────────────────────────────────────

    /// <summary>
    /// Token'ı AKTİF tema sözlüğünden okur. Çizim anında çağrılır — önceden
    /// kopyalanan bir renk tema değişimini kaçırırdı.
    /// Bulunamazsa nötr griye düşer ve çizim çökmez.
    /// </summary>
    private static SKColor Color(string key)
    {
        var resource = System.Windows.Application.Current?.TryFindResource(key);

        if (resource is SolidColorBrush brush)
            return new SKColor(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A);

        return new SKColor(0x64, 0x74, 0x8B);
    }
}
