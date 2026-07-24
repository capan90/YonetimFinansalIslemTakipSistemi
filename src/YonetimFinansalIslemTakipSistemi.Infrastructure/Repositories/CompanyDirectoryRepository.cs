using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

public class CompanyDirectoryRepository : ICompanyDirectoryRepository
{
    private readonly AppDbContext _context;

    public CompanyDirectoryRepository(AppDbContext context) => _context = context;

    public async Task<CompanyDirectory?> GetByIdAsync(Guid id)
        => await _context.CompanyDirectories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<CompanyDirectory?> GetByIdWithTrackingAsync(Guid id)
        => await _context.CompanyDirectories.FirstOrDefaultAsync(x => x.Id == id);

    public async Task<IReadOnlyList<CompanyDirectory>> GetAllAsync()
        => await _context.CompanyDirectories.AsNoTracking().ToListAsync();

    public async Task AddAsync(CompanyDirectory entity)
    {
        await _context.CompanyDirectories.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CompanyDirectory entity)
    {
        _context.CompanyDirectories.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IReadOnlyList<CompanyDirectory> entities)
    {
        if (entities.Count == 0) return;

        // Toplu import ya hep ya hiç (UserPermissionRepository.UpdateAsync deseni)
        await using var tx = await _context.Database.BeginTransactionAsync();
        await _context.CompanyDirectories.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
