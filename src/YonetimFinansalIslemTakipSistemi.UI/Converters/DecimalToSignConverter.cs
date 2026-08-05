using System.Globalization;
using System.Windows.Data;

namespace YonetimFinansalIslemTakipSistemi.UI.Converters;

/// <summary>
/// Sayının işaretini metne çevirir: "Positive" / "Negative" / "Zero".
///
/// NEDEN RENK DÖNDÜRMÜYOR:
/// Converter bağlama başına BİR KEZ çalışır ve döndürdüğü Brush örneği o
/// bağlamada donar. Tema değiştiğinde sözlükteki fırça değişir ama ekranda
/// duran eski fırça yerinde kalır — bakiye rakamları eski temanın renginde
/// takılı kalıyordu. Ayrıca eski hâli renkleri sabit RGB tutuyordu, yani
/// koyu temada hiç doğru renge dönmüyordu.
///
/// Yerine: XAML'de bu değeri okuyan DataTrigger + DynamicResource kullanılır.
/// DataTrigger setter'ındaki DynamicResource tema değişiminde anında güncellenir.
/// Bkz. MainWindow.xaml → BalanceAmount stili.
/// </summary>
[ValueConversion(typeof(decimal), typeof(string))]
public sealed class DecimalToSignConverter : IValueConverter
{
    public const string Positive = "Positive";
    public const string Negative = "Negative";
    public const string Zero     = "Zero";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return d > 0 ? Positive : d < 0 ? Negative : Zero;

        return Zero;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
