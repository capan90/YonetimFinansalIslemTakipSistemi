using Microsoft.EntityFrameworkCore;
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
}
