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
/// Sprint 21: işlem başına taze DbContext (IDbContextFactory) → paylaşılan context çakışması yok.
/// </summary>
public class CashTransactionRepository : ICashTransactionRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public CashTransactionRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<CashTransaction?> GetByIdAsync(Guid id)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.CashTransactions.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task AddAsync(CashTransaction transaction)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        await ctx.CashTransactions.AddAsync(transaction);
        await ctx.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IReadOnlyList<CashTransaction> transactions)
    {
        if (transactions.Count == 0) return;

        // Toplu import ya hep ya hiç — finansal veri kısmi durumda bırakılamaz
        await using var ctx = await _factory.CreateDbContextAsync();
        await using var tx = await ctx.Database.BeginTransactionAsync();
        await ctx.CashTransactions.AddRangeAsync(transactions);
        await ctx.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task UpdateAsync(CashTransaction transaction)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.CashTransactions.Update(transaction);
        await ctx.SaveChangesAsync();
    }

    /// <summary>Fiziksel silme yapmaz; kaydı soft-delete olarak işaretler.</summary>
    public async Task DeleteAsync(Guid id)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var entity = await ctx.CashTransactions.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return;

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<CashTransaction>> GetFilteredAsync(
        DateTime? from, DateTime? to, TransactionType? type, CurrencyType? currency)
    {
        // Salt okuma: liste DTO'ya dönüştürülür, entity izlenmez
        await using var ctx = await _factory.CreateDbContextAsync();
        var query = ctx.CashTransactions.AsNoTracking().AsQueryable();

        if (from.HasValue)     query = query.Where(x => x.TransactionDate >= from.Value);
        if (to.HasValue)       query = query.Where(x => x.TransactionDate <= to.Value);
        if (type.HasValue)     query = query.Where(x => x.TransactionType == type.Value);
        if (currency.HasValue) query = query.Where(x => x.CurrencyType == currency.Value);

        return await query
            .OrderByDescending(x => x.TransactionDate)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<CashTransaction>> GetAllForBalanceAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.CashTransactions
            .AsNoTracking()
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync();
    }

    public async Task<List<CurrencyReportData>> GetReportDataAsync(
        DateTime?        startUtc,
        DateTime?        endExclusiveUtc,
        TransactionType? transactionType    = null,
        CurrencyType?    currencyType       = null,
        string?          descriptionContains = null)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var query = ctx.CashTransactions.AsQueryable();

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
        // Salt okuma: rapor detayı DTO'ya dönüştürülür, entity izlenmez
        await using var ctx = await _factory.CreateDbContextAsync();
        var query = ctx.CashTransactions.AsNoTracking().AsQueryable();

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
