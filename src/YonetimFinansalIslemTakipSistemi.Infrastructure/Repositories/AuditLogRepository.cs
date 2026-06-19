using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(AuditLog log)
    {
        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetFilteredAsync(
        Guid? userId, DateTime? from, DateTime? to, AuditAction? action)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (from.HasValue)
            // Tarih filtresi UTC olarak uygulanır — Timestamp kolonu UTC
            query = query.Where(x => x.Timestamp >= DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc));

        if (to.HasValue)
            // Gün sonu dahil: seçilen günün 23:59:59'u kapsar
            query = query.Where(x => x.Timestamp < DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Utc));

        if (action.HasValue)
            query = query.Where(x => x.Action == action.Value);

        return await query.OrderByDescending(x => x.Timestamp).ToListAsync();
    }
}
