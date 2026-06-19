using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

public class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly AppDbContext _context;

    public ExchangeRateRepository(AppDbContext context) => _context = context;

    public async Task<ExchangeRate?> GetByDateAndCurrencyAsync(DateTime rateDateUtc, CurrencyType currency)
        => await _context.ExchangeRates
            .FirstOrDefaultAsync(e => e.RateDate == rateDateUtc && e.CurrencyType == currency);

    public async Task<IReadOnlyList<ExchangeRate>> GetFilteredAsync(
        DateTime? fromUtc, DateTime? toExclusiveUtc, CurrencyType? currency)
    {
        var query = _context.ExchangeRates.AsQueryable();

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
        await _context.ExchangeRates.AddAsync(rate);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ExchangeRate rate)
    {
        _context.ExchangeRates.Update(rate);
        await _context.SaveChangesAsync();
    }
}
