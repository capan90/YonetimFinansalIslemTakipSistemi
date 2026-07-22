using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

public class WhatsAppContactRepository : IWhatsAppContactRepository
{
    private readonly AppDbContext _context;

    public WhatsAppContactRepository(AppDbContext context) => _context = context;

    public async Task<WhatsAppContact?> GetByIdAsync(Guid id)
        => await _context.WhatsAppContacts.FirstOrDefaultAsync(x => x.Id == id);

    public async Task<WhatsAppContact?> GetByPhoneAsync(string normalizedPhone, bool includeDeleted)
    {
        var query = includeDeleted
            ? _context.WhatsAppContacts.IgnoreQueryFilters()
            : _context.WhatsAppContacts;

        return await query.FirstOrDefaultAsync(x => x.Phone == normalizedPhone);
    }

    public async Task<IReadOnlyList<WhatsAppContact>> GetListAsync(
        string? search, string? company, bool includeInactive)
    {
        var query = _context.WhatsAppContacts.AsNoTracking().AsQueryable();

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
        await _context.WhatsAppContacts.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(WhatsAppContact entity)
    {
        _context.WhatsAppContacts.Update(entity);
        await _context.SaveChangesAsync();
    }
}
