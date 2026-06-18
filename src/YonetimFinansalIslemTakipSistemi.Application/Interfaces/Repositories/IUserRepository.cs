using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

/// <summary>
/// Kullanıcı kayıtlarına erişim sözleşmesi.
/// Infrastructure katmanı bu arayüzü implement eder.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);

    /// <summary>Giriş ekranında kullanıcı adı ile doğrulama için.</summary>
    Task<User?> GetByUserNameAsync(string userName);

    Task<IReadOnlyList<User>> GetAllAsync();

    Task AddAsync(User user);

    Task UpdateAsync(User user);

    /// <summary>Fiziksel silme değil; BaseEntity.IsDeleted alanını işaretler.</summary>
    Task DeleteAsync(Guid id);
}
