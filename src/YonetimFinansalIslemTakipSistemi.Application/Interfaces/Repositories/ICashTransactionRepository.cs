using YonetimFinansalIslemTakipSistemi.Application.Features.Reports.Queries.GetReport;
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

    /// <summary>İşlem tipine göre filtrele (Giriş, Çıkış)</summary>
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

    /// <summary>
    /// Running balance hesabı için tüm aktif kayıtları kronolojik sırada döndürür.
    /// Soft-delete global query filter devrede — silinmiş kayıtlar dahil edilmez.
    /// Sıralama: TransactionDate ASC, CreatedAt ASC, Id ASC (deterministik).
    /// </summary>
    Task<IReadOnlyList<CashTransaction>> GetAllForBalanceAsync();

    /// <summary>
    /// Rapor için GROUP BY aggregate sorgusu.
    /// Soft-delete global query filter devrede — silinmiş kayıtlar dahil edilmez.
    /// Tarih aralığı yarı-açık: >= startUtc AND &lt; endExclusiveUtc.
    /// Her iki parametre null ise tüm aktif kayıtlar üzerinden aggregation yapılır.
    /// transactionType, currencyType ve descriptionContains null ise ilgili filtre uygulanmaz.
    /// </summary>
    Task<List<CurrencyReportData>> GetReportDataAsync(
        DateTime?       startUtc,
        DateTime?       endExclusiveUtc,
        TransactionType? transactionType      = null,
        CurrencyType?   currencyType          = null,
        string?         descriptionContains   = null);

    /// <summary>
    /// Rapor detay görünümü için satır bazlı kayıt listesi.
    /// Aynı filtreler uygulanır; sıralama TransactionDate ASC (bakiye hesabı için).
    /// </summary>
    Task<IReadOnlyList<CashTransaction>> GetFilteredForReportDetailAsync(
        DateTime?       startUtc,
        DateTime?       endExclusiveUtc,
        TransactionType? transactionType,
        CurrencyType?   currencyType,
        string?         descriptionContains);
}
