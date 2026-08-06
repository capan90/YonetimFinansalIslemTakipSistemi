using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;
using Xunit.Abstractions;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Grafik motorunun çalışma zamanında gerçekten ayakta olduğunu doğrular.
///
/// İKİ NEDEN:
///
/// 1. LiveCharts'ın transitif bağımlılıkları (SkiaSharp.Views.WPF, OpenTK)
///    yalnızca .NET Framework hedefliyor ve NU1701 uyarısı üretiyorlar. Uyarı
///    UI/UiTests projelerinde bastırıldı; bastırmanın meşruiyeti bu testlere
///    dayanıyor. Test kırılırsa bastırma da geçersizleşir.
///
/// 2. SkiaSharp NATIVE bileşen taşır (libSkiaSharp). ClickOnce paketlemesinde
///    en sık atlanan yer burasıdır: yönetilen dll'ler pakete girer, native
///    dosyalar girmez ve uygulama yalnızca MÜŞTERİDE, grafik ilk çizilirken
///    patlar. Bu test native yüklemeyi build çıktısı üzerinde doğrular.
/// </summary>
public class ChartRuntimeTests(ITestOutputHelper output)
{
    [Fact]
    public void CartesianChart_olusturulup_olculebiliyor()
    {
        ThemeTestHost.Run(() =>
        {
            var chart = new CartesianChart
            {
                Width  = 400,
                Height = 200,
                Series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = [1, 5, 3, 8, 2],
                        Stroke = new SolidColorPaint(SKColor.Parse("#2A78D6")) { StrokeThickness = 2 },
                        Fill   = null,
                    }
                },
                XAxes = [new Axis { Labels = ["a", "b", "c", "d", "e"] }],
            };

            var host = new Border { Child = chart };
            host.Measure(new Size(400, 200));
            host.Arrange(new Rect(0, 0, 400, 200));

            output.WriteLine($"Chart olusturuldu: {chart.GetType().FullName}");
            output.WriteLine($"Seri sayisi: {chart.Series.Count()}");
            output.WriteLine($"Olculen boyut: {host.DesiredSize}");

            Assert.NotNull(chart);
        });
    }

    [Fact]
    public void SkiaSharp_native_yuklenebiliyor()
    {
        // Native libSkiaSharp yuklenemezse burada patlar
        using var bitmap = new SKBitmap(10, 10);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Red);

        output.WriteLine($"SkiaSharp calisti. Pixel: {bitmap.GetPixel(1, 1)}");
        Assert.Equal(SKColors.Red, bitmap.GetPixel(1, 1));
    }
}
