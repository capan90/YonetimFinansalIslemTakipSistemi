using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using YonetimFinansalIslemTakipSistemi.UI.Common;

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
            return ThemeBrush.Get("Theme.Success");

        return ThemeBrush.Get("Theme.Warning");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
