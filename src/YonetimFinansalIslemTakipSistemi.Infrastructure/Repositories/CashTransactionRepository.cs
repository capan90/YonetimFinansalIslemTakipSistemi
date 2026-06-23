using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Features.Reports.Queries.GetReport;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

/// <summary>
/// ICashTransactionRepository'nin EF Core + PostgreSQL implementasyonu.
/// Soft delete sorgulardan otomatik filtrelenir (AppDbContext global query filter).
/// </summary>
public class CashTransactionRepository : ICashTransactionRepository
{
    private readonly AppDbContext _context;

    public CashTransactionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CashTransaction?> GetByIdAsync(Guid id)
        => await _context.CashTransactions.FirstOrDefaultAsync(x => x.Id == id);

    public async Task<IReadOnlyList<CashTransaction>> GetAllAsync()
        => await _context.CashTransactions.ToListAsync();

    public async Task<IReadOnlyList<CashTransaction>> GetByTypeAsync(TransactionType type)
        => await _context.CashTransactions
            .Where(x => x.TransactionType == type)
            .ToListAsync();

    public async Task<IReadOnlyList<CashTransaction>> GetByCurrencyAsync(CurrencyType currency)
        => await _context.CashTransactions
            .Where(x => x.CurrencyType == currency)
            .ToListAsync();

    public async Task<IReadOnlyList<CashTransaction>> GetByDateRangeAsync(DateTime from, DateTime to)
        => await _context.CashTransactions
            .Where(x => x.TransactionDate >= from && x.TransactionDate <= to)
            .ToListAsync();

    public async Task AddAsync(CashTransaction transaction)
    {
        await _context.CashTransactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CashTransaction transaction)
    {
        _context.CashTransactions.Update(transaction);
        await _context.SaveChangesAsync();
    }

    /// <summary>Fiziksel silme yapmaz; kaydı soft-delete olarak işaretler.</summary>
    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.CashTransactions.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return;

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<CashTransaction>> GetFilteredAsync(
        DateTime? from, DateTime? to, TransactionType? type, CurrencyType? currency)
    {
        var query = _context.CashTransactions.AsQueryable();

        if (from.HasValue)     query = query.Where(x => x.TransactionDate >= from.Value);
        if (to.HasValue)       query = query.Where(x => x.TransactionDate <= to.Value);
        if (type.HasValue)     query = query.Where(x => x.TransactionType == type.Value);
        if (currency.HasValue) query = query.Where(x => x.CurrencyType == currency.Value);

        return await query
            .OrderByDescending(x => x.TransactionDate)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<CashTransaction>> GetAllForBalanceAsync()
        => await _context.CashTransactions
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync();

    public async Task<List<CurrencyReportData>> GetReportDataAsync(
        DateTime?        startUtc,
        DateTime?        endExclusiveUtc,
        TransactionType? transactionType    = null,
        CurrencyType?    currencyType       = null,
        string?          descriptionContains = null)
    {
        var query = _context.CashTransactions.AsQueryable();

        // Yarı-açık aralık: >= start, < endExclusive
        if (startUtc.HasValue)         query = query.Where(t => t.TransactionDate >= startUtc.Value);
        if (endExclusiveUtc.HasValue)  query = query.Where(t => t.TransactionDate <  endExclusiveUtc.Value);
        if (transactionType.HasValue)  query = query.Where(t => t.TransactionType == transactionType.Value);
        if (currencyType.HasValue)     query = query.Where(t => t.CurrencyType    == currencyType.Value);

        // Açıklama filtresi — PostgreSQL'de büyük/küçük harf duyarsız içerir araması
        if (!string.IsNullOrEmpty(descriptionContains))
            query = query.Where(t => t.Description != null &&
                                     t.Description.ToLower().Contains(descriptionContains.ToLower()));

        // GROUP BY PostgreSQL'de çalışır; kayıtların tamamı belleğe çekilmez
        return await query
            .GroupBy(t => new { t.CurrencyType, t.TransactionType })
            .Select(g => new CurrencyReportData(
                g.Key.CurrencyType,
                g.Key.TransactionType,
                g.Sum(t => t.Amount),
                g.Count()))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<CashTransaction>> GetFilteredForReportDetailAsync(
        DateTime?        startUtc,
        DateTime?        endExclusiveUtc,
        TransactionType? transactionType,
        CurrencyType?    currencyType,
        string?          descriptionContains)
    {
        var query = _context.CashTransactions.AsQueryable();

        if (startUtc.HasValue)        query = query.Where(t => t.TransactionDate >= startUtc.Value);
        if (endExclusiveUtc.HasValue) query = query.Where(t => t.TransactionDate <  endExclusiveUtc.Value);
        if (transactionType.HasValue) query = query.Where(t => t.TransactionType == transactionType.Value);
        if (currencyType.HasValue)    query = query.Where(t => t.CurrencyType    == currencyType.Value);

        if (!string.IsNullOrEmpty(descriptionContains))
            query = query.Where(t => t.Description != null &&
                                     t.Description.ToLower().Contains(descriptionContains.ToLower()));

        // Bakiye hesabı için artan sıra zorunludur
        return await query
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .ToListAsync();
    }
}
