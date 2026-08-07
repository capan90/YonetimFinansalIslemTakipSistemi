namespace YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

/// <summary>
/// Kapanmadan önce söz hakkı isteyen kabuk ekranları bunu uygular.
///
/// Kaydedilmemiş değişiklik kontrolü için açık kapı: ekran
/// <see cref="RequestClose"/>'dan <c>false</c> dönerse sekme kapatılmaz ve
/// logout iptal edilir. Uygulaması ZORUNLU DEĞİL — çoğu liste ekranının
/// kaydedilmemiş durumu yoktur ve bu arayüzü uygulamaz.
///
/// Kabuk, onay diyaloğunu kendisi göstermez: hangi mesajın gösterileceğini
/// ekran bilir (IDialogService üzerinden). Kabuk yalnızca kararı sorar.
/// </summary>
public interface IShellScreen
{
    /// <summary>
    /// Ekran kapanmaya hazır mı. <c>false</c> dönerse kapatma iptal edilir.
    /// Kullanıcıya sorulacaksa bu metot içinde sorulur.
    /// </summary>
    bool RequestClose();
}
