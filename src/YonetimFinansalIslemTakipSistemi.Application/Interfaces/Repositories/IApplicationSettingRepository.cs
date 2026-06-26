using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

public interface IApplicationSettingRepository
{
    Task<ApplicationSetting?> GetByKeyAsync(string key);
    Task<IReadOnlyList<ApplicationSetting>> GetByPrefixAsync(string prefix);
    /// <summary>Key varsa günceller; yoksa yeni kayıt oluşturur.</summary>
    Task UpsertAsync(string key, string? value, bool isEncrypted, Guid userId);
}
