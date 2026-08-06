namespace YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

/// <summary>
/// Kendi içinde "Çıkış Yap" düğmesi taşıyan ekranlar bunu uygular.
///
/// NEDEN VAR: Nakit İşlemler ekranının araç çubuğunda bugün bir çıkış düğmesi
/// var ve MainWindow onu dinliyor. Aynı ekran kabuk içinde barındığında bu
/// düğmenin sessizce ölü kalmaması gerekiyor — ama kabuk da hangi ekranın
/// böyle bir düğmesi olduğunu bilmemeli.
///
/// Kapsam bilinçli olarak TEK ÜYE: ekran yalnızca HABER VERİR. Onay, audit ve
/// pencerenin kapatılması barındıran pencerenin işidir; sözleşme
/// <c>IsLogoutRequested</c> tarafında hiç değişmez.
/// </summary>
public interface IShellLogoutSource
{
    /// <summary>Kullanıcı ekran içinden çıkış istedi.</summary>
    event Action? LogoutRequested;
}
