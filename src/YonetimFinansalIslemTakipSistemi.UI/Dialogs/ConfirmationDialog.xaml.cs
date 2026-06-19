using System.Windows;

namespace YonetimFinansalIslemTakipSistemi.UI.Dialogs;

public partial class ConfirmationDialog : Window
{
    public string HeaderTitle { get; }
    public string Message     { get; }

    public ConfirmationDialog(string title, string message)
    {
        HeaderTitle = title;
        Message     = message;

        InitializeComponent();
        DataContext = this;
        Title = title;
    }

    private void YesButton_Click(object sender, RoutedEventArgs e)  => DialogResult = true;
    private void NoButton_Click(object sender, RoutedEventArgs e)   => DialogResult = false;

    // X butonu kapatılırsa DialogResult null kalır — ShowDialog() == true ifadesi false verir
}
