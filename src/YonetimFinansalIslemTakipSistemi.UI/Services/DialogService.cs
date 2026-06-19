using System.Windows;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.Dialogs;

namespace YonetimFinansalIslemTakipSistemi.UI.Services;

/// <summary>
/// WPF tabanlı IDialogService implementasyonu.
/// Her çağrıda yeni pencere nesnesi oluşturur; singleton olarak kaydedilir.
/// </summary>
public class DialogService : IDialogService
{
    public void ShowInfo(string message, string title = "Bilgi")
        => Open(new MessageDialog(DialogType.Info, title, message));

    public void ShowSuccess(string message, string title = "Başarılı")
        => Open(new MessageDialog(DialogType.Success, title, message));

    public void ShowWarning(string message, string title = "Uyarı")
        => Open(new MessageDialog(DialogType.Warning, title, message));

    public void ShowError(string message, string title = "Hata")
        => Open(new MessageDialog(DialogType.Error, title, message));

    public bool ShowConfirmation(string message, string title = "Onay")
    {
        var dialog = new ConfirmationDialog(title, message) { Owner = GetActiveWindow() };
        // X ile kapatılırsa DialogResult null → false
        return dialog.ShowDialog() == true;
    }

    private static void Open(MessageDialog dialog)
    {
        dialog.Owner = GetActiveWindow();
        dialog.ShowDialog();
    }

    // O an aktif olan pencereyi owner olarak döner; dialog ana pencerenin arkasında kalmaz
    // System.Windows.Application — YonetimFinansalIslemTakipSistemi.Application ile çakışmaması için tam ad
    private static Window? GetActiveWindow()
        => System.Windows.Application.Current.Windows
               .OfType<Window>()
               .FirstOrDefault(w => w.IsActive)
           ?? System.Windows.Application.Current.MainWindow;
}
