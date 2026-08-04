using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

// WhatsAppContactRepository ile aynı desen: işlem başına taze DbContext (IDbContextFactory).
public class MailContactRepository : IMailContactRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public MailContactRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<MailContact?> GetByIdAsync(Guid id)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.MailContacts.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<MailContact?> GetByEmailAsync(string normalizedEmail, bool includeDeleted)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var query = includeDeleted
            ? ctx.MailContacts.IgnoreQueryFilters()
            : ctx.MailContacts;

        return await query.FirstOrDefaultAsync(x => x.Email == normalizedEmail);
    }

    public async Task<IReadOnlyList<MailContact>> GetListAsync(string? search, bool includeInactive)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var query = ctx.MailContacts.AsNoTracking().AsQueryable();

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Ad, e-posta veya firma üzerinden büyük/küçük harf duyarsız arama
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.FullName, pattern) ||
                EF.Functions.ILike(x.Email, pattern) ||
                (x.Company != null && EF.Functions.ILike(x.Company, pattern)));
        }

        // Son kullanılan üstte; hiç kullanılmamışlar ada göre arkada
        return await query
            .OrderByDescending(x => x.LastUsedAt.HasValue)
            .ThenByDescending(x => x.LastUsedAt)
            .ThenBy(x => x.FullName)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<MailContact>> GetDefaultCcAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.MailContacts
            .AsNoTracking()
            .Where(x => x.IsDefaultCc && x.IsActive)
            .OrderBy(x => x.FullName)
            .ToListAsync();
    }

    public async Task AddAsync(MailContact entity)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        await ctx.MailContacts.AddAsync(entity);
        await ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(MailContact entity)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.MailContacts.Update(entity);
        await ctx.SaveChangesAsync();
    }

    public async Task TouchLastUsedAsync(IReadOnlyCollection<string> normalizedEmails, DateTime usedAtUtc)
    {
        if (normalizedEmails.Count == 0) return;

        await using var ctx = await _factory.CreateDbContextAsync();
        var matches = await ctx.MailContacts
            .Where(x => normalizedEmails.Contains(x.Email))
            .ToListAsync();

        if (matches.Count == 0) return;   // manuel yazılan adres rehberde yoksa iş yok

        foreach (var contact in matches)
            contact.LastUsedAt = usedAtUtc;

        await ctx.SaveChangesAsync();
    }
}
