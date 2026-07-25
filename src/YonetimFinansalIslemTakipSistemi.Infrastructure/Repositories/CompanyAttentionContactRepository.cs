using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

// Sprint 21: işlem başına taze DbContext (IDbContextFactory).
public class CompanyAttentionContactRepository : ICompanyAttentionContactRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public CompanyAttentionContactRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<List<CompanyAttentionContact>> GetByCompanyAsync(Guid companyDirectoryId)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.CompanyAttentionContacts
            .AsNoTracking()
            .Where(x => x.CompanyDirectoryId == companyDirectoryId)
            .ToListAsync();
    }

    public async Task AddAsync(CompanyAttentionContact contact)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        await ctx.CompanyAttentionContacts.AddAsync(contact);
        await ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(CompanyAttentionContact contact)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.CompanyAttentionContacts.Update(contact);
        await ctx.SaveChangesAsync();
    }
}
