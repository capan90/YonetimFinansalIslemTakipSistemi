using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.Converters;

/// <summary>
/// SystemLogLevel → Badge arka plan rengi.
/// ConverterParameter = "foreground" verilirse metin rengi döner.
///
/// Renkler sabit RGB idi (Brushes.Black, Brushes.White, elle yazılmış RGB);
/// koyu temada log satırları okunmuyordu. Artık tema token'larından çözülür.
/// </summary>
[ValueConversion(typeof(SystemLogLevel), typeof(Brush))]
public sealed class SystemLogLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isForeground = "foreground".Equals(parameter as string, StringComparison.OrdinalIgnoreCase);

        if (value is not SystemLogLevel level)
            return Neutral(isForeground);

        var (fg, bg) = level switch
        {
            SystemLogLevel.Info     => ("Theme.Info.Text",    "Theme.Info.BackgroundStrong"),
            SystemLogLevel.Warning  => ("Theme.Warning.Text", "Theme.Warning.BackgroundStrong"),
            SystemLogLevel.Error    => ("Theme.Danger.Text",  "Theme.Danger.BackgroundStrong"),
            // Kritik en yüksek vurgu: dolu kırmızı zemin + üzerine okunan metin
            SystemLogLevel.Critical => ("Theme.OnDanger",     "Theme.Danger"),
            _                       => (null, null)
        };

        if (fg is null) return Neutral(isForeground);

        return ThemeBrush.Get(isForeground ? fg : bg!);
    }

    private static Brush Neutral(bool isForeground) =>
        ThemeBrush.Get(isForeground ? "Theme.Text" : "Theme.SurfaceAlt");

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
