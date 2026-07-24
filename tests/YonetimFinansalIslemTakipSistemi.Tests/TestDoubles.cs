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

    public Task WriteRangeAsync(IReadOnlyList<AuditEntry> entries)
    {
        lock (Entries)
            foreach (var e in entries)
                Entries.Add((e.Action, e.OldValues, e.NewValues));
        return Task.CompletedTask;
    }
}

internal sealed class FakeSystemLogService : ISystemLogService
{
    public List<(string Level, string Category, string Message, Exception? Exception)> Entries { get; } = [];

    public Task LogInfoAsync(string category, string message, string? source = null)
    { Entries.Add(("Info", category, message, null)); return Task.CompletedTask; }

    public Task LogWarningAsync(string category, string message, string? source = null)
    { Entries.Add(("Warning", category, message, null)); return Task.CompletedTask; }

    public Task LogErrorAsync(string category, string message, Exception? exception = null, string? source = null)
    { Entries.Add(("Error", category, message, exception)); return Task.CompletedTask; }

    public Task LogCriticalAsync(string category, string message, Exception? exception = null, string? source = null)
    { Entries.Add(("Critical", category, message, exception)); return Task.CompletedTask; }

    public Task<Application.Features.SystemLogs.PagedSystemLogResultDto> SearchAsync(
        Application.Features.SystemLogs.SystemLogSearchQuery query)
        => Task.FromResult(new Application.Features.SystemLogs.PagedSystemLogResultDto());

    public Task<Application.Features.SystemLogs.SystemLogDetailDto?> GetByIdAsync(Guid id)
        => Task.FromResult<Application.Features.SystemLogs.SystemLogDetailDto?>(null);

    public Task MarkResolvedAsync(Guid id, Guid resolvedByUserId, string? note) => Task.CompletedTask;
}

internal sealed class FakeCargoDashboardCache : ICargoDashboardCacheService
{
    public int InvalidateCount { get; private set; }

    public Application.Features.CargoShipment.Queries.GetCargoDashboard.CargoDashboardDto? Get() => null;
    public void Set(Application.Features.CargoShipment.Queries.GetCargoDashboard.CargoDashboardDto dto) { }
    public void Invalidate() => InvalidateCount++;
}

internal sealed class FakeCompanyDirectoryRepository : ICompanyDirectoryRepository
{
    public List<CompanyDirectory> Items { get; } = [];

    public Task<CompanyDirectory?> GetByIdAsync(Guid id)
        => Task.FromResult(Items.FirstOrDefault(x => x.Id == id && !x.IsDeleted));

    public Task<CompanyDirectory?> GetByIdWithTrackingAsync(Guid id) => GetByIdAsync(id);

    public Task<IReadOnlyList<CompanyDirectory>> GetAllAsync()
        => Task.FromResult<IReadOnlyList<CompanyDirectory>>(Items.Where(x => !x.IsDeleted).ToList());

    public Task AddAsync(CompanyDirectory entity)
    {
        Items.Add(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CompanyDirectory entity) => Task.CompletedTask;

    /// <summary>true ise sıradaki toplu ekleme, transaction rollback'ini simüle ederek exception fırlatır.</summary>
    public bool FailNextAddRange { get; set; }

    public Task AddRangeAsync(IReadOnlyList<CompanyDirectory> entities)
    {
        if (FailNextAddRange)
        {
            FailNextAddRange = false;
            throw new InvalidOperationException("Simulated import failure");
        }
        Items.AddRange(entities);
        return Task.CompletedTask;
    }
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

    public Task AddRangeWithAutoNumberAsync(IReadOnlyList<CargoShipment> entities)
    {
        if (entities.Count == 0) return Task.CompletedTask;

        var direction = entities[0].Direction;
        if (entities.Any(e => e.Direction != direction))
            throw new ArgumentException("Toplu içe aktarmada tüm kayıtlar aynı yönde olmalıdır.", nameof(entities));

        // Gerçek repo gibi: sayaç tek seferde +N artar, aralık dağıtılır, hepsi atomik
        lock (_counterLock)
        {
            if (FailNextAddRange)
            {
                FailNextAddRange = false;
                throw new InvalidOperationException("Simulated batch failure");
            }

            var last  = _counters.GetValueOrDefault(direction) + entities.Count;
            _counters[direction] = last;
            var first = last - entities.Count + 1;

            for (var i = 0; i < entities.Count; i++)
            {
                entities[i].ShipmentNumber = CargoNumberFormatter.Format(direction, first + i);
                if (!_usedNumbers.Add(entities[i].ShipmentNumber!))
                    throw new InvalidOperationException($"Duplicate shipment number: {entities[i].ShipmentNumber}");
                Added.Add(entities[i]);
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>true ise sıradaki toplu ekleme, transaction rollback'ini simüle ederek exception fırlatır.</summary>
    public bool FailNextAddRange { get; set; }

    public Task<IReadOnlyList<CargoShipment>> GetActiveForImportCheckAsync(
        CargoShipmentDirection direction, DateTime fromUtc, DateTime toUtc)
        => Task.FromResult<IReadOnlyList<CargoShipment>>(AllRecords
            .Where(x => !x.IsDeleted
                     && x.Direction == direction
                     && x.ShipmentDate >= fromUtc
                     && x.ShipmentDate <= toUtc)
            .ToList());

    public Task<IReadOnlyList<CargoShipment>> GetByTrackingNumbersAsync(
        CargoShipmentDirection direction, IReadOnlyCollection<string> trackingNumbers)
    {
        var keys = trackingNumbers.Select(t => t.Trim().ToUpperInvariant()).ToHashSet();
        return Task.FromResult<IReadOnlyList<CargoShipment>>(AllRecords
            .Where(x => !x.IsDeleted
                     && x.Direction == direction
                     && x.TrackingNumber is not null
                     && keys.Contains(x.TrackingNumber.Trim().ToUpperInvariant()))
            .ToList());
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
    {
        // Gerçek repository'nin SQL filtre/sıralamasını in-memory taklit eder
        var query = AllRecords.Where(x => !x.IsDeleted);
        if (dateFrom.HasValue)           query = query.Where(x => x.ShipmentDate >= dateFrom.Value.Date);
        if (dateTo.HasValue)             query = query.Where(x => x.ShipmentDate <= dateTo.Value.Date);
        if (direction.HasValue)          query = query.Where(x => x.Direction == direction.Value);
        if (cargoCompanyId.HasValue)     query = query.Where(x => x.CargoCompanyId == cargoCompanyId.Value);
        if (status.HasValue)             query = query.Where(x => x.Status == status.Value);
        if (notificationStatus.HasValue) query = query.Where(x => x.NotificationStatus == notificationStatus.Value);
        if (priority.HasValue)           query = query.Where(x => x.Priority == priority.Value);

        return Task.FromResult<IReadOnlyList<CargoShipment>>(query
            .OrderByDescending(x => x.ShipmentDate)
            .ThenByDescending(x => x.CreatedAt)
            .ToList());
    }
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

    public Task<IReadOnlyList<WhatsAppContact>> GetAllForImportAsync()
        => Task.FromResult<IReadOnlyList<WhatsAppContact>>(Items.ToList()); // soft delete dahil

    /// <summary>true ise sıradaki toplu kayıt, transaction rollback'ini simüle ederek exception fırlatır.</summary>
    public bool FailNextSaveImport { get; set; }

    public Task SaveImportAsync(IReadOnlyList<WhatsAppContact> toAdd, IReadOnlyList<WhatsAppContact> toUpdate)
    {
        if (FailNextSaveImport)
        {
            FailNextSaveImport = false;
            throw new InvalidOperationException("Simulated import failure");
        }
        Items.AddRange(toAdd);
        // toUpdate zaten Items içindeki referanslardır — mutasyonları görünür
        return Task.CompletedTask;
    }
}

/// <summary>Harf dönüşümü uygulamayan basit normalizasyon — yalnızca trim (Preserve davranışı).</summary>
internal sealed class FakeTextNormalizationService : IUserTextNormalizationService
{
    public string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal sealed class FakeCashTransactionRepository : ICashTransactionRepository
{
    public List<CashTransaction> Items { get; } = [];

    public Task<CashTransaction?> GetByIdAsync(Guid id)
        => Task.FromResult(Items.FirstOrDefault(x => x.Id == id && !x.IsDeleted));

    public Task AddAsync(CashTransaction transaction)
    {
        Items.Add(transaction);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CashTransaction transaction) => Task.CompletedTask;

    public Task DeleteAsync(Guid id)
    {
        var entity = Items.FirstOrDefault(x => x.Id == id);
        if (entity is not null) entity.IsDeleted = true;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CashTransaction>> GetFilteredAsync(
        DateTime? from, DateTime? to,
        TransactionType? type, CurrencyType? currency)
    {
        var query = Items.Where(x => !x.IsDeleted);
        if (from.HasValue)     query = query.Where(x => x.TransactionDate >= from.Value);
        if (to.HasValue)       query = query.Where(x => x.TransactionDate <= to.Value);
        if (type.HasValue)     query = query.Where(x => x.TransactionType == type.Value);
        if (currency.HasValue) query = query.Where(x => x.CurrencyType == currency.Value);
        return Task.FromResult<IReadOnlyList<CashTransaction>>(query.ToList());
    }

    public Task<IReadOnlyList<CashTransaction>> GetAllForBalanceAsync()
        => Task.FromResult<IReadOnlyList<CashTransaction>>(Items
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.TransactionDate).ThenBy(x => x.CreatedAt).ThenBy(x => x.Id)
            .ToList());

    public Task<List<Application.Features.Reports.Queries.GetReport.CurrencyReportData>> GetReportDataAsync(
        DateTime? startUtc, DateTime? endExclusiveUtc,
        TransactionType? transactionType = null, CurrencyType? currencyType = null,
        string? descriptionContains = null)
        => Task.FromResult(new List<Application.Features.Reports.Queries.GetReport.CurrencyReportData>());

    public Task<IReadOnlyList<CashTransaction>> GetFilteredForReportDetailAsync(
        DateTime? startUtc, DateTime? endExclusiveUtc,
        TransactionType? transactionType, CurrencyType? currencyType, string? descriptionContains)
        => Task.FromResult<IReadOnlyList<CashTransaction>>([]);
}

internal sealed class FakeUserRepository : IUserRepository
{
    public List<User> Items { get; } = [];

    public Task<User?> GetByIdAsync(Guid id)
        => Task.FromResult(Items.FirstOrDefault(x => x.Id == id && !x.IsDeleted));

    public Task<User?> GetByUserNameAsync(string userName)
        => Task.FromResult(Items.FirstOrDefault(x =>
            !x.IsDeleted && string.Equals(x.UserName, userName, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<User>> GetAllAsync()
        => Task.FromResult<IReadOnlyList<User>>(Items.Where(x => !x.IsDeleted).ToList());

    public Task AddAsync(User user)
    {
        Items.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user) => Task.CompletedTask;

    public Task DeleteAsync(Guid id)
    {
        var entity = Items.FirstOrDefault(x => x.Id == id);
        if (entity is not null) entity.IsDeleted = true;
        return Task.CompletedTask;
    }
}

internal sealed class FakeUserPermissionRepository : IUserPermissionRepository
{
    public Dictionary<Guid, HashSet<PermissionType>> Map { get; } = [];

    public Task<IReadOnlySet<PermissionType>> GetByUserIdAsync(Guid userId)
        => Task.FromResult<IReadOnlySet<PermissionType>>(
            Map.TryGetValue(userId, out var set) ? set : new HashSet<PermissionType>());

    public Task UpdateAsync(Guid userId, IEnumerable<PermissionType> permissions)
    {
        Map[userId] = [.. permissions];
        return Task.CompletedTask;
    }

    public Task<bool> AnyOtherActiveUserHasPermissionAsync(PermissionType permission, Guid excludeUserId)
        => Task.FromResult(Map.Any(kv => kv.Key != excludeUserId && kv.Value.Contains(permission)));
}
