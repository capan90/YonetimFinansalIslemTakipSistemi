using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Features.SystemLogs;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// Sistem olaylarını ve uygulama hatalarını SystemLogs tablosuna yazar.
/// Singleton; her DB yazma için kısa ömürlü IServiceScope açar.
/// IUserContext ve IErrorNotificationService IServiceProvider üzerinden lazy resolve edilir.
/// Tüm iç hatalar yutulur — log yazma asla uygulamayı çökertmez.
/// </summary>
public sealed class SystemLogService : ISystemLogService
{
    private readonly IServiceProvider _services;
    private static readonly string?  _appVersion =
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

    public SystemLogService(IServiceProvider services)
        => _services = services;

    public Task LogInfoAsync(string category, string message, string? source = null)
        => WriteAsync(SystemLogLevel.Info, category, message, null, source);

    public Task LogWarningAsync(string category, string message, string? source = null)
        => WriteAsync(SystemLogLevel.Warning, category, message, null, source);

    public Task LogErrorAsync(string category, string message, Exception? exception = null, string? source = null)
        => WriteAsync(SystemLogLevel.Error, category, message, exception, source);

    /// <summary>
    /// DB'ye Critical log yazar, ardından fire-and-forget mail tetikler.
    /// Mail hata verirse ayrıca hata loglama yapılmaz (sonsuz döngü riski).
    /// </summary>
    public async Task LogCriticalAsync(string category, string message, Exception? exception = null, string? source = null)
    {
        await WriteAsync(SystemLogLevel.Critical, category, message, exception, source);

        _ = Task.Run(async () =>
        {
            try
            {
                var notifier = _services.GetService<IErrorNotificationService>();
                if (notifier is null) return;
                var context = new NotificationContext
                {
                    Level    = "Critical",
                    Screen   = source ?? "Unknown",
                    UserId   = TryGetUserId()?.ToString(),
                    UserName = TryGetUsername()
                };
                await notifier.NotifyAsync(message, exception, context);
            }
            catch { /* mail başarısız — sonsuz döngüsü oluşmaması için sessizce geç */ }
        });
    }

    public async Task<PagedSystemLogResultDto> SearchAsync(SystemLogSearchQuery query)
    {
        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var q = db.SystemLogs.AsNoTracking().AsQueryable();

            if (query.DateFrom.HasValue)
                q = q.Where(x => x.CreatedAt >= query.DateFrom.Value.Date.ToUniversalTime());
            if (query.DateTo.HasValue)
                q = q.Where(x => x.CreatedAt < query.DateTo.Value.Date.AddDays(1).ToUniversalTime());
            if (query.Level.HasValue)
                q = q.Where(x => x.Level == query.Level.Value);
            if (!string.IsNullOrWhiteSpace(query.Category))
                q = q.Where(x => x.Category == query.Category);
            if (query.IsResolved.HasValue)
                q = q.Where(x => x.IsResolved == query.IsResolved.Value);
            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                var kw = query.SearchText.Trim().ToLower();
                q = q.Where(x => x.Message.ToLower().Contains(kw)
                              || (x.ExceptionType != null && x.ExceptionType.ToLower().Contains(kw))
                              || (x.Username      != null && x.Username.ToLower().Contains(kw)));
            }

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(x => x.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedSystemLogResultDto
            {
                Items      = items.Select(MapToListItem).ToList(),
                TotalCount = total,
                Page       = query.Page,
                PageSize   = query.PageSize
            };
        }
        catch
        {
            return new PagedSystemLogResultDto { Page = query.Page, PageSize = query.PageSize };
        }
    }

    public async Task<SystemLogDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var log = await db.SystemLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return log is null ? null : MapToDetail(log);
        }
        catch { return null; }
    }

    public async Task MarkResolvedAsync(Guid id, Guid resolvedByUserId, string? note)
    {
        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var log = await db.SystemLogs.FindAsync(id);
            if (log is null) return;

            log.IsResolved       = true;
            log.ResolvedAt       = DateTime.UtcNow;
            log.ResolvedByUserId = resolvedByUserId;
            log.ResolutionNote   = note;
            await db.SaveChangesAsync();
        }
        catch { /* çözüm işareti başarısız olsa da UI'ı çökertme */ }
    }

    // ── İç yardımcılar ──────────────────────────────────────────────────────

    private async Task WriteAsync(
        SystemLogLevel level,
        string         category,
        string         message,
        Exception?     exception,
        string?        source)
    {
        var log = new SystemLog
        {
            Id                    = Guid.NewGuid(),
            Level                 = level,
            Category              = category,
            Message               = message,
            ExceptionType         = exception?.GetType().FullName,
            StackTrace            = exception?.StackTrace,
            InnerExceptionMessage = exception?.InnerException?.Message,
            Source                = source,
            UserId                = TryGetUserId(),
            Username              = TryGetUsername(),
            MachineName           = Environment.MachineName,
            AppVersion            = _appVersion,
            IsCritical            = level == SystemLogLevel.Critical,
            IsResolved            = false,
            CreatedAt             = DateTime.UtcNow
        };

        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SystemLogs.Add(log);
            await db.SaveChangesAsync();
        }
        catch { /* DB yazma başarısız olsa uygulama devam etmeli */ }
    }

    private Guid? TryGetUserId()
    {
        try
        {
            var id = _services.GetService<IUserContext>()?.UserId;
            return id == Guid.Empty ? null : id;
        }
        catch { return null; }
    }

    private string? TryGetUsername()
    {
        try { return _services.GetService<IUserContext>()?.FullName; }
        catch { return null; }
    }

    private static SystemLogListItemDto MapToListItem(SystemLog x) => new()
    {
        Id            = x.Id,
        Level         = x.Level,
        LevelDisplay  = DisplayLevel(x.Level),
        Category      = x.Category,
        Message       = x.Message.Length > 200 ? x.Message[..200] + "…" : x.Message,
        Username      = x.Username,
        MachineName   = x.MachineName,
        IsCritical    = x.IsCritical,
        IsResolved    = x.IsResolved,
        StatusDisplay = x.IsResolved ? "Çözüldü" : "Açık",
        CreatedAt     = x.CreatedAt
    };

    private static SystemLogDetailDto MapToDetail(SystemLog x) => new()
    {
        Id                    = x.Id,
        Level                 = x.Level,
        LevelDisplay          = DisplayLevel(x.Level),
        Category              = x.Category,
        Message               = x.Message,
        ExceptionType         = x.ExceptionType,
        StackTrace            = x.StackTrace,
        InnerExceptionMessage = x.InnerExceptionMessage,
        Source                = x.Source,
        UserId                = x.UserId,
        Username              = x.Username,
        MachineName           = x.MachineName,
        AppVersion            = x.AppVersion,
        IsCritical            = x.IsCritical,
        IsResolved            = x.IsResolved,
        ResolvedAt            = x.ResolvedAt,
        ResolvedByUserId      = x.ResolvedByUserId,
        ResolutionNote        = x.ResolutionNote,
        CreatedAt             = x.CreatedAt
    };

    internal static string DisplayLevel(SystemLogLevel level) => level switch
    {
        SystemLogLevel.Info     => "Bilgi",
        SystemLogLevel.Warning  => "Uyarı",
        SystemLogLevel.Error    => "Hata",
        SystemLogLevel.Critical => "Kritik",
        _                       => level.ToString()
    };
}
