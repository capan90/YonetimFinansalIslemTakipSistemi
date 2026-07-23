using Microsoft.EntityFrameworkCore;
using Npgsql;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

public class CargoShipmentRepository : ICargoShipmentRepository
{
    private readonly AppDbContext _context;
    private readonly ISystemLogService _systemLog;

    public CargoShipmentRepository(AppDbContext context, ISystemLogService systemLog)
    {
        _context   = context;
        _systemLog = systemLog;
    }

    public async Task<CargoShipment?> GetByIdAsync(Guid id)
        => await _context.CargoShipments.FirstOrDefaultAsync(x => x.Id == id);

    public async Task<CargoShipment?> GetByIdWithIncludesAsync(Guid id)
        => await _context.CargoShipments
            .Include(x => x.CargoCompany)
            .Include(x => x.CompanyDirectory)
            .FirstOrDefaultAsync(x => x.Id == id);

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

    public async Task AddWithAutoNumberAsync(CargoShipment entity)
    {
        // Sayaç artışı ve insert tek transaction'dadır:
        //  - UPDATE ... RETURNING satır kilidiyle eşzamanlı eklemeleri sıraya sokar (mükerrer imkansız)
        //  - insert başarısız olursa rollback sayaç artışını da geri alır (numara boşa gitmez)
        //  - sayaç kayıt tablosundan bağımsızdır: soft delete edilen numaralar asla yeniden kullanılmaz
        for (var attempt = 1; ; attempt++)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // NOT: INSERT ... RETURNING, EF SqlQuery ile okunamaz (non-composable SQL);
                // artış ExecuteSql ile yapılır, değer aynı transaction içinde ayrıca okunur.
                // Upsert satır kilidi commit'e kadar tutulur → okuma ve eşzamanlılık güvenlidir.
                await _context.Database.ExecuteSqlAsync($@"
                    INSERT INTO cargo_number_counters (""Direction"", ""LastValue"")
                    VALUES ({(int)entity.Direction}, 1)
                    ON CONFLICT (""Direction"")
                    DO UPDATE SET ""LastValue"" = cargo_number_counters.""LastValue"" + 1");

                var seq = await _context.CargoNumberCounters
                    .AsNoTracking()
                    .Where(c => c.Direction == entity.Direction)
                    .Select(c => c.LastValue)
                    .SingleAsync();

                entity.ShipmentNumber = CargoNumberFormatter.Format(entity.Direction, seq);

                await _context.CargoShipments.AddAsync(entity);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
            {
                // Migration uygulanmamış — kullanıcıya anlaşılır teknik yönlendirme,
                // gerçek hata InnerException'da korunur (handler System Log'a yazar)
                throw new Application.Common.DataStoreException(
                    "Veritabanı şeması güncel değil: 'cargo_number_counters' tablosu bulunamadı. " +
                    "Lütfen uygulamayı yeniden başlatın (migration açılışta uygulanır) veya " +
                    "'20260722083350_AddUserPrefsWhatsAppDirectoryAndCargoCounters' migration'ını uygulayın.", ex);
            }
            catch (DbUpdateException ex) when (attempt < 3 && ex.InnerException is PostgresException { SqlState: "23505" })
            {
                // Sayaç mevcut verinin gerisindeyse (ör. elle taşınmış kayıt) unique index yakalar.
                // Sayaç mevcut max'a senkronlanır ve yeniden denenir.
                _context.Entry(entity).State = EntityState.Detached;
                await tx.RollbackAsync();
                await ResyncCounterAsync(entity.Direction);

                // Normal akışta hiç yaşanmaması gereken bir durum — teknik iz bırakılır
                await _systemLog.LogWarningAsync(
                    "CargoNumber",
                    $"Kargo numarası çakışması yakalandı; sayaç yeniden senkronlandı (deneme {attempt}, yön {entity.Direction}).",
                    source: nameof(CargoShipmentRepository));
            }
        }
    }

    public async Task AddRangeWithAutoNumberAsync(IReadOnlyList<CargoShipment> entities)
    {
        if (entities.Count == 0) return;

        var direction = entities[0].Direction;
        if (entities.Any(e => e.Direction != direction))
            throw new ArgumentException("Toplu içe aktarmada tüm kayıtlar aynı yönde olmalıdır.", nameof(entities));

        // AddWithAutoNumberAsync ile aynı atomik desen; fark: sayaç TEK seferde +N
        // artırılarak numara aralığı rezerve edilir ve tüm kayıtlar tek transaction'da
        // eklenir (ya hep ya hiç). Rollback'te sayaç artışı da geri döner — boşluk oluşmaz.
        // Sayaç satırı kilidi commit'e kadar tutulur; eşzamanlı tekil eklemeler kısa süre bekler.
        for (var attempt = 1; ; attempt++)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlAsync($@"
                    INSERT INTO cargo_number_counters (""Direction"", ""LastValue"")
                    VALUES ({(int)direction}, {entities.Count})
                    ON CONFLICT (""Direction"")
                    DO UPDATE SET ""LastValue"" = cargo_number_counters.""LastValue"" + {entities.Count}");

                var last = await _context.CargoNumberCounters
                    .AsNoTracking()
                    .Where(c => c.Direction == direction)
                    .Select(c => c.LastValue)
                    .SingleAsync();

                var first = last - entities.Count + 1;
                for (var i = 0; i < entities.Count; i++)
                    entities[i].ShipmentNumber = CargoNumberFormatter.Format(direction, first + i);

                await _context.CargoShipments.AddRangeAsync(entities);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
            {
                throw new Application.Common.DataStoreException(
                    "Veritabanı şeması güncel değil: 'cargo_number_counters' tablosu bulunamadı. " +
                    "Lütfen uygulamayı yeniden başlatın (migration açılışta uygulanır) veya " +
                    "'20260722083350_AddUserPrefsWhatsAppDirectoryAndCargoCounters' migration'ını uygulayın.", ex);
            }
            catch (DbUpdateException ex) when (attempt < 3 && ex.InnerException is PostgresException { SqlState: "23505" })
            {
                // Sayaç mevcut verinin gerisindeyse unique index yakalar — senkronla ve yeniden dene
                foreach (var entity in entities)
                    _context.Entry(entity).State = EntityState.Detached;
                await tx.RollbackAsync();
                await ResyncCounterAsync(direction);

                await _systemLog.LogWarningAsync(
                    "CargoNumber",
                    $"Toplu içe aktarmada numara çakışması yakalandı; sayaç yeniden senkronlandı (deneme {attempt}, yön {direction}).",
                    source: nameof(CargoShipmentRepository));
            }
        }
    }

    public async Task<IReadOnlyList<CargoShipment>> GetActiveForImportCheckAsync(
        CargoShipmentDirection direction, DateTime fromUtc, DateTime toUtc)
        => await _context.CargoShipments
            .AsNoTracking()
            .Where(x => x.Direction == direction
                     && x.ShipmentDate >= fromUtc
                     && x.ShipmentDate <= toUtc)
            .ToListAsync();

    public async Task<IReadOnlyList<CargoShipment>> GetByTrackingNumbersAsync(
        CargoShipmentDirection direction, IReadOnlyCollection<string> trackingNumbers)
    {
        // Karşılaştırma anahtarı: TRIM + UPPER — analiz tarafındaki normalize ile aynı
        var keys = trackingNumbers.Select(t => t.Trim().ToUpperInvariant()).ToList();

        return await _context.CargoShipments
            .AsNoTracking()
            .Where(x => x.Direction == direction
                     && x.TrackingNumber != null
                     && keys.Contains(x.TrackingNumber.Trim().ToUpper()))
            .ToListAsync();
    }

    public async Task<long?> SoftDeleteWithNumberReclaimAsync(CargoShipment entity)
    {
        // İş kuralı: yalnızca yönün EN SON numarası silinirse numara geri kullanılabilir;
        // aradaki silinmiş numaralar asla geri dönmez. Eski format (G/C-YYYY-NNNN) sayaca dokunmaz.
        var seq = CargoNumberFormatter.TryParseSequence(entity.ShipmentNumber, entity.Direction);
        long? reclaimed = null;

        await using var tx = await _context.Database.BeginTransactionAsync();

        if (seq is not null)
        {
            var prefix = CargoNumberFormatter.Prefix(entity.Direction);

            // Koşullu geri alma tek atomik UPDATE ile yapılır; satır kilidi eşzamanlı
            // create'leri (aynı sayaç satırındaki ON CONFLICT UPDATE) commit'e kadar bekletir:
            //  - sayaç, silinen kaydın sıra değerine eşitse VE
            //  - aynı yönde (soft delete dahil) daha yüksek numaralı kayıt yoksa → sayaç 1 geri alınır
            var affected = await _context.Database.ExecuteSqlAsync($@"
                UPDATE cargo_number_counters c
                SET ""LastValue"" = c.""LastValue"" - 1
                WHERE c.""Direction"" = {(int)entity.Direction}
                  AND c.""LastValue"" = {seq.Value}
                  AND NOT EXISTS (
                      SELECT 1 FROM cargo_shipments s
                      WHERE s.""Id"" <> {entity.Id}
                        AND s.""ShipmentNumber"" ~ ('^' || {prefix} || '[0-9]+$')
                        AND substring(s.""ShipmentNumber"" from 4)::bigint > {seq.Value})");

            if (affected == 1)
            {
                // Numara unique index'ten serbest bırakılır ki sonraki kayıt aynı numarayı
                // alabilsin. Numara bilgisi silme audit kaydında korunur (handler yazar).
                entity.ShipmentNumber = null;
                reclaimed = seq;
            }
        }

        // Soft delete ve (varsa) sayaç geri alma aynı transaction'da:
        // rollback durumunda sayaç değişmeden kalır
        _context.CargoShipments.Update(entity);
        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        if (reclaimed is not null)
            await _systemLog.LogInfoAsync(
                "CargoNumber",
                $"Silinen son kargo numarası geri alındı: {CargoNumberFormatter.Format(entity.Direction, reclaimed.Value)} " +
                $"— sonraki kayıt bu numarayı yeniden kullanacak (yön {entity.Direction}).",
                source: nameof(CargoShipmentRepository));

        return reclaimed;
    }

    /// <summary>Sayacı, o yöndeki mevcut en büyük numara sırasına eşitler (defansif onarım).</summary>
    private async Task ResyncCounterAsync(CargoShipmentDirection direction)
    {
        var prefix = CargoNumberFormatter.Prefix(direction);
        await _context.Database.ExecuteSqlAsync($@"
            UPDATE cargo_number_counters c
            SET ""LastValue"" = GREATEST(c.""LastValue"", COALESCE((
                SELECT MAX(substring(""ShipmentNumber"" from 4)::bigint)
                FROM cargo_shipments
                WHERE ""ShipmentNumber"" ~ ('^' || {prefix} || '[0-9]+$')), 0))
            WHERE ""Direction"" = {(int)direction}");
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

        // timestamptz sütunu için Kind=Utc zorunlu — Npgsql 6+ strict mod
        if (dateFrom.HasValue)
            query = query.Where(x => x.ShipmentDate >= DateTime.SpecifyKind(dateFrom.Value.Date, DateTimeKind.Utc));

        if (dateTo.HasValue)
            query = query.Where(x => x.ShipmentDate <= DateTime.SpecifyKind(dateTo.Value.Date, DateTimeKind.Utc));

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
