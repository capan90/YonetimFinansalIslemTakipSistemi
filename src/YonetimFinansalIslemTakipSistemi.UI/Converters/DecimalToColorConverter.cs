using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace YonetimFinansalIslemTakipSistemi.UI.Converters;

// Pozitif → yeşil, negatif → kırmızı, sıfır → gri (bakiye kartlarında kullanılır)
public class DecimalToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush Green = new(Color.FromRgb(0x2E, 0x7D, 0x32));
    private static readonly SolidColorBrush Red   = new(Color.FromRgb(0xC6, 0x28, 0x28));
    private static readonly SolidColorBrush Gray  = new(Color.FromRgb(0x64, 0x74, 0x8B));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return d > 0 ? Green : d < 0 ? Red : Gray;
        return Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
