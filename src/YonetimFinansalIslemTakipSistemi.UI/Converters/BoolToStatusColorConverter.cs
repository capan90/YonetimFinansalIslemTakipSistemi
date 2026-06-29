using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace YonetimFinansalIslemTakipSistemi.UI.Converters;

/// <summary>
/// IsResolved bool → "Çözüldü" (yeşil) / "Açık" (turuncu) rengi.
/// </summary>
[ValueConversion(typeof(bool), typeof(Brush))]
public sealed class BoolToStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool resolved && resolved)
            return new SolidColorBrush(Color.FromRgb(22, 163, 74));   // yeşil

        return new SolidColorBrush(Color.FromRgb(234, 88, 12));       // turuncu
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
