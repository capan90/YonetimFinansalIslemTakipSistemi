using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

public class CompanyAttentionContactRepository : ICompanyAttentionContactRepository
{
    private readonly AppDbContext _context;

    public CompanyAttentionContactRepository(AppDbContext context) => _context = context;

    public async Task<List<CompanyAttentionContact>> GetByCompanyAsync(Guid companyDirectoryId)
        => await _context.CompanyAttentionContacts
            .AsNoTracking()
            .Where(x => x.CompanyDirectoryId == companyDirectoryId)
            .ToListAsync();

    public async Task AddAsync(CompanyAttentionContact contact)
    {
        await _context.CompanyAttentionContacts.AddAsync(contact);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CompanyAttentionContact contact)
    {
        _context.CompanyAttentionContacts.Update(contact);
        await _context.SaveChangesAsync();
    }
}
