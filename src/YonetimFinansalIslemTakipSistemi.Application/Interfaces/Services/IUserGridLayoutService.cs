namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Kullanıcı başına DataGrid kolon düzenini saklayan servis sözleşmesi.
/// ScreenKey ile farklı ekranlar için ayrı düzenler tutulabilir (ör. "CashTransactionList").
/// </summary>
public interface IUserGridLayoutService
{
    Task<string?> GetLayoutAsync(Guid userId, string screenKey);
    Task SaveLayoutAsync(Guid userId, string screenKey, string layoutJson);

    /// <summary>
    /// Kullanıcının ilgili ekrana ait kayıtlı düzenini siler ("Varsayılan Tasarıma Dön" için).
    /// Kayıt bulunamazsa sessizce döner.
    /// </summary>
    Task DeleteLayoutAsync(Guid userId, string screenKey);
}
