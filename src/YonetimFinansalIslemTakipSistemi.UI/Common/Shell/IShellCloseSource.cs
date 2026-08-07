namespace YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

/// <summary>
/// Kendi içinde "Kapat" düğmesi (veya Esc kısayolu) taşıyan ekranlar bunu uygular.
///
/// NEDEN VAR: Pencere dünyasında bu düğme <c>Close()</c> çağırıyordu. Ekran
/// artık hem ince barındırıcı pencerede hem kabuk sekmesinde durabiliyor ve
/// "kapan" iki yerde farklı şey demek: pencerede pencereyi kapatmak, kabukta
/// sekmeyi kapatmak. Ekran bunu bilmemeli, yalnızca İSTEĞİ yaymalı.
///
/// <see cref="IShellLogoutSource"/> ile aynı desen: ekran haber verir, karar
/// barındırana aittir.
/// </summary>
public interface IShellCloseSource
{
    /// <summary>Kullanıcı bu ekranın kapanmasını istedi.</summary>
    event Action? CloseRequested;
}
