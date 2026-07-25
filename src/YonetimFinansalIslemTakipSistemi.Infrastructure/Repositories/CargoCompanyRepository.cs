using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

// Sprint 21: işlem başına taze DbContext (IDbContextFactory).
public class CargoCompanyRepository : ICargoCompanyRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public CargoCompanyRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<CargoCompany?> GetByIdAsync(Guid id)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.CargoCompanies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<CargoCompany?> GetByIdWithTrackingAsync(Guid id)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.CargoCompanies.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IReadOnlyList<CargoCompany>> GetAllAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.CargoCompanies.AsNoTracking().ToListAsync();
    }

    public async Task AddAsync(CargoCompany entity)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        await ctx.CargoCompanies.AddAsync(entity);
        await ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(CargoCompany entity)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.CargoCompanies.Update(entity);
        await ctx.SaveChangesAsync();
    }
}
