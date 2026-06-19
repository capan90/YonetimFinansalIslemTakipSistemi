namespace YonetimFinansalIslemTakipSistemi.UI.Abstractions;

public interface IDialogService
{
    void ShowInfo(string message, string title = "Bilgi");
    void ShowSuccess(string message, string title = "Başarılı");
    void ShowWarning(string message, string title = "Uyarı");
    void ShowError(string message, string title = "Hata");

    /// <summary>
    /// Kullanıcıdan onay ister. X ile kapatılırsa false döner.
    /// </summary>
    bool ShowConfirmation(string message, string title = "Onay");
}
