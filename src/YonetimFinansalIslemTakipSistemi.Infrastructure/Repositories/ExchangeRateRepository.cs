using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

// Sprint 21: işlem başına taze DbContext (IDbContextFactory).
public class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ExchangeRateRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<ExchangeRate?> GetByDateAndCurrencyAsync(DateTime rateDateUtc, CurrencyType currency)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.ExchangeRates
            .FirstOrDefaultAsync(e => e.RateDate == rateDateUtc && e.CurrencyType == currency);
    }

    public async Task<IReadOnlyList<ExchangeRate>> GetFilteredAsync(
        DateTime? fromUtc, DateTime? toExclusiveUtc, CurrencyType? currency)
    {
        // Salt okuma: liste görüntüleme; upsert akışı GetByDateAndCurrencyAsync (tracked) kullanır
        await using var ctx = await _factory.CreateDbContextAsync();
        var query = ctx.ExchangeRates.AsNoTracking().AsQueryable();

        if (fromUtc.HasValue)        query = query.Where(e => e.RateDate >= fromUtc.Value);
        if (toExclusiveUtc.HasValue) query = query.Where(e => e.RateDate <  toExclusiveUtc.Value);
        if (currency.HasValue)       query = query.Where(e => e.CurrencyType == currency.Value);

        // En yeni tarih üstte; aynı tarihte USD önce EUR sonra
        return await query
            .OrderByDescending(e => e.RateDate)
            .ThenBy(e => e.CurrencyType)
            .ToListAsync();
    }

    public async Task AddAsync(ExchangeRate rate)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        await ctx.ExchangeRates.AddAsync(rate);
        await ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(ExchangeRate rate)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.ExchangeRates.Update(rate);
        await ctx.SaveChangesAsync();
    }
}
