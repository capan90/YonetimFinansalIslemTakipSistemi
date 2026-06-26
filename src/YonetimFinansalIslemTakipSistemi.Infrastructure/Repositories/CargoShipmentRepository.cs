using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

public class CargoShipmentRepository : ICargoShipmentRepository
{
    private readonly AppDbContext _context;

    public CargoShipmentRepository(AppDbContext context) => _context = context;

    public async Task<CargoShipment?> GetByIdAsync(Guid id)
        => await _context.CargoShipments.FirstOrDefaultAsync(x => x.Id == id);

    public async Task<CargoShipment?> GetByIdWithIncludesAsync(Guid id)
        => await _context.CargoShipments
            .Include(x => x.CargoCompany)
            .Include(x => x.CompanyDirectory)
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<IReadOnlyList<CargoShipment>> GetByDirectionAsync(CargoShipmentDirection direction)
        => await _context.CargoShipments
            .AsNoTracking()
            .Include(x => x.CargoCompany)
            .Include(x => x.CompanyDirectory)
            .Where(x => x.Direction == direction)
            .ToListAsync();

    public async Task AddAsync(CargoShipment entity)
    {
        await _context.CargoShipments.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CargoShipment entity)
    {
        _context.CargoShipments.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<string> GetNextShipmentNumberAsync(CargoShipmentDirection direction, int year)
    {
        var prefix = direction == CargoShipmentDirection.Incoming ? "C" : "G";
        var pattern = $"{prefix}-{year}-";

        // Silinmiş kayıtlar dahil — bir kez kullanılan numara tekrar kullanılmamalı
        var existing = await _context.CargoShipments
            .IgnoreQueryFilters()
            .Where(x => x.ShipmentNumber != null && x.ShipmentNumber.StartsWith(pattern))
            .Select(x => x.ShipmentNumber!)
            .ToListAsync();

        int maxSeq = 0;
        foreach (var num in existing)
        {
            var parts = num.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int seq))
                maxSeq = Math.Max(maxSeq, seq);
        }

        return $"{prefix}-{year}-{(maxSeq + 1):D4}";
    }

    public async Task<IReadOnlyList<CargoShipment>> GetAllActiveAsync()
        => await _context.CargoShipments
            .AsNoTracking()
            .Include(x => x.CargoCompany)
            .Include(x => x.CompanyDirectory)
            .ToListAsync();

    public async Task<IReadOnlyList<CargoShipment>> GetRecentAsync(int count)
        => await _context.CargoShipments
            .AsNoTracking()
            .Include(x => x.CargoCompany)
            .Include(x => x.CompanyDirectory)
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .ToListAsync();

    public async Task<IReadOnlyList<CargoShipment>> GetFilteredReportAsync(
        DateTime?                dateFrom,
        DateTime?                dateTo,
        CargoShipmentDirection?  direction,
        Guid?                    cargoCompanyId,
        CargoShipmentStatus?     status,
        CargoNotificationStatus? notificationStatus,
        CargoShipmentPriority?   priority)
    {
        var query = _context.CargoShipments
            .AsNoTracking()
            .Include(x => x.CargoCompany)
            .Include(x => x.CompanyDirectory)
            .AsQueryable();

        if (dateFrom.HasValue)
            query = query.Where(x => x.ShipmentDate >= dateFrom.Value.Date);

        if (dateTo.HasValue)
            query = query.Where(x => x.ShipmentDate <= dateTo.Value.Date);

        if (direction.HasValue)
            query = query.Where(x => x.Direction == direction.Value);

        if (cargoCompanyId.HasValue)
            query = query.Where(x => x.CargoCompanyId == cargoCompanyId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (notificationStatus.HasValue)
            query = query.Where(x => x.NotificationStatus == notificationStatus.Value);

        if (priority.HasValue)
            query = query.Where(x => x.Priority == priority.Value);

        return await query
            .OrderByDescending(x => x.ShipmentDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
}
