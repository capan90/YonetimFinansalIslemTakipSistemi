using Microsoft.EntityFrameworkCore;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

// Sprint 21: işlem başına taze DbContext (IDbContextFactory).
public class AuditLogRepository : IAuditLogRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public AuditLogRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task AddAsync(AuditLog log)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        await ctx.AuditLogs.AddAsync(log);
        await ctx.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IReadOnlyList<AuditLog> logs)
    {
        // Toplu import: binlerce kayıt tek SaveChanges ile yazılır — kayıt başına
        // round-trip UI'ı dondurur
        await using var ctx = await _factory.CreateDbContextAsync();
        await ctx.AuditLogs.AddRangeAsync(logs);
        await ctx.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetFilteredAsync(
        Guid? userId, DateTime? from, DateTime? to, AuditAction? action)
    {
        // Salt okuma: denetim ekranı görüntülemesi, entity izlenmez
        await using var ctx = await _factory.CreateDbContextAsync();
        var query = ctx.AuditLogs.AsNoTracking().AsQueryable();

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
