using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

// Sprint 21: işlem başına taze DbContext (IDbContextFactory).
public class CompanyDirectoryRepository : ICompanyDirectoryRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public CompanyDirectoryRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<CompanyDirectory?> GetByIdAsync(Guid id)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.CompanyDirectories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<CompanyDirectory?> GetByIdWithTrackingAsync(Guid id)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.CompanyDirectories.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IReadOnlyList<CompanyDirectory>> GetAllAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.CompanyDirectories.AsNoTracking().ToListAsync();
    }

    public async Task AddAsync(CompanyDirectory entity)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        await ctx.CompanyDirectories.AddAsync(entity);
        await ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(CompanyDirectory entity)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.CompanyDirectories.Update(entity);
        await ctx.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IReadOnlyList<CompanyDirectory> entities)
    {
        if (entities.Count == 0) return;

        // Toplu import ya hep ya hiç
        await using var ctx = await _factory.CreateDbContextAsync();
        await using var tx = await ctx.Database.BeginTransactionAsync();
        await ctx.CompanyDirectories.AddRangeAsync(entities);
        await ctx.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
