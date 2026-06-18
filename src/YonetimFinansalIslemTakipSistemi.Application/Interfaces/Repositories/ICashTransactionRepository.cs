using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

/// <summary>
/// Nakit işlem kayıtlarına erişim sözleşmesi.
/// Infrastructure katmanı bu arayüzü implement eder.
/// </summary>
public interface ICashTransactionRepository
{
    Task<CashTransaction?> GetByIdAsync(Guid id);

    Task<IReadOnlyList<CashTransaction>> GetAllAsync();

    /// <summary>İşlem tipine göre filtrele (Tahsilat, Ödeme vb.)</summary>
    Task<IReadOnlyList<CashTransaction>> GetByTypeAsync(TransactionType type);

    /// <summary>Para birimine göre filtrele (TRY, USD, EUR)</summary>
    Task<IReadOnlyList<CashTransaction>> GetByCurrencyAsync(CurrencyType currency);

    /// <summary>Tarih aralığına göre filtrele; rapor ekranı için kullanılır.</summary>
    Task<IReadOnlyList<CashTransaction>> GetByDateRangeAsync(DateTime from, DateTime to);

    Task AddAsync(CashTransaction transaction);

    Task UpdateAsync(CashTransaction transaction);

    /// <summary>Fiziksel silme değil; BaseEntity.IsDeleted alanını işaretler.</summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Opsiyonel filtrelerle kayıt listesi döndürür. Null olan filtreler görmezden gelinir.
    /// Soft delete global query filter'ı devrede — IsDeleted burada tekrar yazılmaz.
    /// </summary>
    Task<IReadOnlyList<CashTransaction>> GetFilteredAsync(
        DateTime? from, DateTime? to,
        TransactionType? type, CurrencyType? currency);
}
