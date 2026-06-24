using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

public class CargoCompanyRepository : ICargoCompanyRepository
{
    private readonly AppDbContext _context;

    public CargoCompanyRepository(AppDbContext context) => _context = context;

    public async Task<CargoCompany?> GetByIdAsync(Guid id)
        => await _context.CargoCompanies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<CargoCompany?> GetByIdWithTrackingAsync(Guid id)
        => await _context.CargoCompanies.FirstOrDefaultAsync(x => x.Id == id);

    public async Task<IReadOnlyList<CargoCompany>> GetAllAsync()
        => await _context.CargoCompanies.AsNoTracking().ToListAsync();

    public async Task AddAsync(CargoCompany entity)
    {
        await _context.CargoCompanies.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CargoCompany entity)
    {
        _context.CargoCompanies.Update(entity);
        await _context.SaveChangesAsync();
    }
}
