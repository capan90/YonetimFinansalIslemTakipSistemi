using System.Windows;
using System.Windows.Media;

namespace YonetimFinansalIslemTakipSistemi.UI.Dialogs;

public enum DialogType { Info, Success, Warning, Error }

public partial class MessageDialog : Window
{
    public string DialogIcon  { get; }
    public string HeaderTitle { get; }
    public string Message     { get; }
    public Brush  HeaderBrush { get; }

    public MessageDialog(DialogType type, string title, string message)
    {
        DialogIcon  = IconFor(type);
        HeaderTitle = title;
        Message     = message;
        HeaderBrush = BrushFor(type);

        InitializeComponent();
        DataContext = this;
        Title = title;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) => Close();

    private static string IconFor(DialogType type) => type switch
    {
        DialogType.Success => "✓",
        DialogType.Warning => "⚠",
        DialogType.Error   => "✕",
        _                  => "ℹ"
    };

    private static Brush BrushFor(DialogType type) => type switch
    {
        DialogType.Success => new SolidColorBrush(Color.FromRgb(39,  174, 96)),
        DialogType.Warning => new SolidColorBrush(Color.FromRgb(230, 126, 34)),
        DialogType.Error   => new SolidColorBrush(Color.FromRgb(192, 57,  43)),
        _                  => new SolidColorBrush(Color.FromRgb(41,  128, 185))
    };
}
