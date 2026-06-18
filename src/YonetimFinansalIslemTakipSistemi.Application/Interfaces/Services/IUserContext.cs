namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Oturum açmış kullanıcının kimlik bilgisine erişim sözleşmesi.
/// Başarılı girişten sonra UserContext singleton'ı bu interface üzerinden okunur.
/// </summary>
public interface IUserContext
{
    Guid UserId { get; }
    string FullName { get; }
}
