using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.UI.Converters;

/// <summary>
/// SystemLogLevel → Badge arka plan rengi.
/// ConverterParameter = "foreground" verilirse metin rengi döner.
/// </summary>
[ValueConversion(typeof(SystemLogLevel), typeof(Brush))]
public sealed class SystemLogLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isForeground = "foreground".Equals(parameter as string, StringComparison.OrdinalIgnoreCase);

        if (value is not SystemLogLevel level)
            return isForeground ? Brushes.Black : Brushes.LightGray;

        return level switch
        {
            SystemLogLevel.Info     => isForeground ? new SolidColorBrush(Color.FromRgb(29, 78, 216))
                                                    : new SolidColorBrush(Color.FromRgb(219, 234, 254)),
            SystemLogLevel.Warning  => isForeground ? new SolidColorBrush(Color.FromRgb(146, 64, 14))
                                                    : new SolidColorBrush(Color.FromRgb(254, 243, 199)),
            SystemLogLevel.Error    => isForeground ? new SolidColorBrush(Color.FromRgb(153, 27, 27))
                                                    : new SolidColorBrush(Color.FromRgb(254, 226, 226)),
            SystemLogLevel.Critical => isForeground ? Brushes.White
                                                    : new SolidColorBrush(Color.FromRgb(220, 38, 38)),
            _                       => isForeground ? Brushes.Black : Brushes.LightGray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
