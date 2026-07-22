using System.Collections.Concurrent;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Handler testleri için basit in-memory sahte implementasyonlar.
/// DB gerektiren davranışlar (satır kilidi, unique index) atomik yapıyla simüle edilir.
/// </summary>
internal sealed class FakeUserContext : IUserContext, IUserSession
{
    private HashSet<PermissionType> _permissions = [];

    public Guid   UserId   { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = "Test Kullanıcı";
    public TextCasePreference TextCasePreference { get; set; } = TextCasePreference.Preserve;

    public IReadOnlySet<PermissionType> Permissions => _permissions;

    public bool HasPermission(PermissionType permission) => _permissions.Contains(permission);

    public void GrantAll() => _permissions = [.. Enum.GetValues<PermissionType>()];

    public void SetUser(Guid userId, string fullName, IReadOnlySet<PermissionType> permissions)
    {
        UserId = userId;
        FullName = fullName;
        _permissions = [.. permissions];
    }

    public void SetTextCasePreference(TextCasePreference preference) => TextCasePreference = preference;

    public void Clear() { }
}

internal sealed class FakeAuditLogService : IAuditLogService
{
    public List<(AuditAction Action, string? OldValues, string? NewValues)> Entries { get; } = [];

    public Task WriteAsync(AuditAction action, Guid userId, string userName,
        string entityType, Guid? entityId, string? oldValues = null, string? newValues = null)
    {
        lock (Entries)
            Entries.Add((action, oldValues, newValues));
        return Task.CompletedTask;
    }
}

internal sealed class FakeCargoDashboardCache : ICargoDashboardCacheService
{
    public Application.Features.CargoShipment.Queries.GetCargoDashboard.CargoDashboardDto? Get() => null;
    public void Set(Application.Features.CargoShipment.Queries.GetCargoDashboard.CargoDashboardDto dto) { }
    public void Invalidate() { }
}

/// <summary>
/// Gerçek repository'nin atomik numara üretimini ve kontrollü geri almasını simüle eder.
/// PostgreSQL'deki sayaç satır kilidinin karşılığı olarak tüm sayaç işlemleri tek lock
/// altında serileşir; unique index numara kümesiyle temsil edilir.
/// </summary>
internal sealed class FakeCargoShipmentRepository : ICargoShipmentRepository
{
    private readonly object _counterLock = new();
    private readonly Dictionary<CargoShipmentDirection, long> _counters = [];
    private readonly HashSet<string> _usedNumbers = [];

    public ConcurrentBag<CargoShipment> Added { get; } = [];
    public List<CargoShipment> Existing { get; } = [];

    /// <summary>true ise sıradaki silme, transaction rollback'ini simüle ederek exception fırlatır.</summary>
    public bool FailNextDelete { get; set; }

    public long GetCounter(CargoShipmentDirection direction)
    {
        lock (_counterLock)
            return _counters.GetValueOrDefault(direction);
    }

    private IEnumerable<CargoShipment> AllRecords => Added.Concat(Existing);

    public Task<CargoShipment?> GetByIdAsync(Guid id)
        => Task.FromResult(AllRecords.FirstOrDefault(x => x.Id == id && !x.IsDeleted));

    public Task<CargoShipment?> GetByIdWithIncludesAsync(Guid id) => GetByIdAsync(id);

    public Task<IReadOnlyList<CargoShipment>> GetByDirectionAsync(CargoShipmentDirection direction)
        => Task.FromResult<IReadOnlyList<CargoShipment>>(
            Existing.Where(x => x.Direction == direction).ToList());

    public Task AddAsync(CargoShipment entity)
    {
        Added.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CargoShipment entity) => Task.CompletedTask;

    public Task AddWithAutoNumberAsync(CargoShipment entity)
    {
        // Satır kilidi simülasyonu: create ve delete aynı kilitte serileşir
        lock (_counterLock)
        {
            var seq = _counters.GetValueOrDefault(entity.Direction) + 1;
            _counters[entity.Direction] = seq;
            entity.ShipmentNumber = CargoNumberFormatter.Format(entity.Direction, seq);

            // Unique index simülasyonu: aynı numara ikinci kez eklenemez
            if (!_usedNumbers.Add(entity.ShipmentNumber))
                throw new InvalidOperationException($"Duplicate shipment number: {entity.ShipmentNumber}");

            Added.Add(entity);
        }
        return Task.CompletedTask;
    }

    public Task<long?> SoftDeleteWithNumberReclaimAsync(CargoShipment entity)
    {
        lock (_counterLock)
        {
            // Rollback simülasyonu: hiçbir değişiklik uygulanmadan çıkılır
            if (FailNextDelete)
            {
                FailNextDelete = false;
                throw new InvalidOperationException("Simulated transaction rollback");
            }

            long? reclaimed = null;
            var seq = CargoNumberFormatter.TryParseSequence(entity.ShipmentNumber, entity.Direction);

            if (seq is not null)
            {
                var last = _counters.GetValueOrDefault(entity.Direction);
                var hasHigher = AllRecords.Any(x =>
                    x.Id != entity.Id &&
                    CargoNumberFormatter.TryParseSequence(x.ShipmentNumber, entity.Direction) > seq);

                // Yalnızca son numara VE daha yüksek kayıt yoksa geri alınır
                if (last == seq.Value && !hasHigher)
                {
                    _usedNumbers.Remove(entity.ShipmentNumber!);
                    entity.ShipmentNumber = null;
                    _counters[entity.Direction] = seq.Value - 1;
                    reclaimed = seq;
                }
            }

            entity.IsDeleted = true;
            return Task.FromResult(reclaimed);
        }
    }

    public Task<IReadOnlyList<CargoShipment>> GetAllActiveAsync()
        => Task.FromResult<IReadOnlyList<CargoShipment>>(Existing.Where(x => !x.IsDeleted).ToList());

    public Task<IReadOnlyList<CargoShipment>> GetRecentAsync(int count)
        => Task.FromResult<IReadOnlyList<CargoShipment>>(Existing.Take(count).ToList());

    public Task<IReadOnlyList<CargoShipment>> GetFilteredReportAsync(
        DateTime? dateFrom, DateTime? dateTo, CargoShipmentDirection? direction,
        Guid? cargoCompanyId, CargoShipmentStatus? status,
        CargoNotificationStatus? notificationStatus, CargoShipmentPriority? priority)
        => Task.FromResult<IReadOnlyList<CargoShipment>>([]);
}

internal sealed class FakeCargoCompanyRepository : ICargoCompanyRepository
{
    public List<CargoCompany> Items { get; } = [];

    public Task<CargoCompany?> GetByIdAsync(Guid id)
        => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

    public Task<CargoCompany?> GetByIdWithTrackingAsync(Guid id) => GetByIdAsync(id);

    public Task<IReadOnlyList<CargoCompany>> GetAllAsync()
        => Task.FromResult<IReadOnlyList<CargoCompany>>(Items.ToList());

    public Task AddAsync(CargoCompany entity)
    {
        Items.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CargoCompany entity) => Task.CompletedTask;
}

internal sealed class FakeUserPreferenceRepository : IUserPreferenceRepository
{
    public List<UserPreference> Items { get; } = [];

    public Task<UserPreference?> GetByUserIdAsync(Guid userId)
        => Task.FromResult(Items.FirstOrDefault(x => x.UserId == userId && !x.IsDeleted));

    public Task AddAsync(UserPreference entity)
    {
        Items.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(UserPreference entity) => Task.CompletedTask;
}

internal sealed class FakeWhatsAppContactRepository : IWhatsAppContactRepository
{
    public List<WhatsAppContact> Items { get; } = [];

    public Task<WhatsAppContact?> GetByIdAsync(Guid id)
        => Task.FromResult(Items.FirstOrDefault(x => x.Id == id && !x.IsDeleted));

    public Task<WhatsAppContact?> GetByPhoneAsync(string normalizedPhone, bool includeDeleted)
        => Task.FromResult(Items.FirstOrDefault(x =>
            x.Phone == normalizedPhone && (includeDeleted || !x.IsDeleted)));

    public Task<IReadOnlyList<WhatsAppContact>> GetListAsync(string? search, string? company, bool includeInactive)
    {
        var query = Items.Where(x => !x.IsDeleted);
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(company)) query = query.Where(x => x.Company == company);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x =>
                x.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.Phone.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (x.Company?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        return Task.FromResult<IReadOnlyList<WhatsAppContact>>(query.ToList());
    }

    public Task AddAsync(WhatsAppContact entity)
    {
        Items.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(WhatsAppContact entity) => Task.CompletedTask;
}
