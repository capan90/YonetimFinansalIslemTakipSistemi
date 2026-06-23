namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

/// <summary>
/// Kullanıcı bazlı DataGrid kolon düzeni kalıcılığı için depo sözleşmesi.
/// </summary>
public interface IUserGridLayoutRepository
{
    Task<string?> GetLayoutJsonAsync(Guid userId, string screenKey);
    Task SaveLayoutJsonAsync(Guid userId, string screenKey, string layoutJson);

    /// <summary>
    /// Kullanıcının ilgili ekrana ait kayıtlı düzenini siler.
    /// Kayıt bulunamazsa sessizce döner.
    /// </summary>
    Task DeleteLayoutAsync(Guid userId, string screenKey);
}
