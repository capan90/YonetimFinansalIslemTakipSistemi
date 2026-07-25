using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

// Sprint 21: İşlem başına taze DbContext (IDbContextFactory) → paylaşılan context
// eşzamanlılık hatası ("A second operation was started...") ortadan kalkar.
public class WhatsAppContactRepository : IWhatsAppContactRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public WhatsAppContactRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<WhatsAppContact?> GetByIdAsync(Guid id)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.WhatsAppContacts.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<WhatsAppContact?> GetByPhoneAsync(string normalizedPhone, bool includeDeleted)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var query = includeDeleted
            ? ctx.WhatsAppContacts.IgnoreQueryFilters()
            : ctx.WhatsAppContacts;

        return await query.FirstOrDefaultAsync(x => x.Phone == normalizedPhone);
    }

    public async Task<IReadOnlyList<WhatsAppContact>> GetListAsync(
        string? search, string? company, bool includeInactive)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var query = ctx.WhatsAppContacts.AsNoTracking().AsQueryable();

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(company))
            query = query.Where(x => x.Company == company);

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Ad, telefon veya firma üzerinden büyük/küçük harf duyarsız arama
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.FullName, pattern) ||
                EF.Functions.ILike(x.Phone, pattern) ||
                (x.Company != null && EF.Functions.ILike(x.Company, pattern)));
        }

        return await query.OrderBy(x => x.FullName).ToListAsync();
    }

    public async Task AddAsync(WhatsAppContact entity)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        await ctx.WhatsAppContacts.AddAsync(entity);
        await ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(WhatsAppContact entity)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.WhatsAppContacts.Update(entity);
        await ctx.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<WhatsAppContact>> GetAllForImportAsync()
    {
        // Soft delete dahil: silinmiş numara import'ta geri yüklenir (create akışıyla aynı)
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.WhatsAppContacts
            .IgnoreQueryFilters()
            .ToListAsync();
    }

    public async Task SaveImportAsync(
        IReadOnlyList<WhatsAppContact> toAdd, IReadOnlyList<WhatsAppContact> toUpdate)
    {
        // Toplu import ya hep ya hiç: ekleme ve geri yüklemeler tek transaction'da
        await using var ctx = await _factory.CreateDbContextAsync();
        await using var tx = await ctx.Database.BeginTransactionAsync();

        if (toAdd.Count > 0)
            await ctx.WhatsAppContacts.AddRangeAsync(toAdd);
        foreach (var entity in toUpdate)
            ctx.WhatsAppContacts.Update(entity);

        await ctx.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
