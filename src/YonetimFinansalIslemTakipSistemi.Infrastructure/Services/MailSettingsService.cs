using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YonetimFinansalIslemTakipSistemi.Application.Features.Settings.MailSettings;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// Mail SMTP ayarlarını application_settings tablosundan okur.
/// Singleton olarak kaydedilir; IServiceScopeFactory aracılığıyla Scoped repository'e erişir.
/// </summary>
public class MailSettingsService : IMailSettingsService
{
    private readonly IServiceScopeFactory          _scopeFactory;
    private readonly ISecretProtector              _protector;
    private readonly ILogger<MailSettingsService>  _logger;
    private readonly ISystemLogService             _systemLog;

    public MailSettingsService(
        IServiceScopeFactory         scopeFactory,
        ISecretProtector             protector,
        ILogger<MailSettingsService> logger,
        ISystemLogService            systemLog)
    {
        _scopeFactory = scopeFactory;
        _protector    = protector;
        _logger       = logger;
        _systemLog    = systemLog;
    }

    public async Task<MailSettingsDto?> GetAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var userContext = scope.ServiceProvider.GetService<IUserContext>();
            var repo = scope.ServiceProvider.GetRequiredService<IApplicationSettingRepository>();

            if (userContext is not null && userContext.UserId != Guid.Empty)
            {
                var personal = await GetInternalAsync(repo, $"UserMail:{userContext.UserId}:");
                if (personal is not null && !string.IsNullOrWhiteSpace(personal.SmtpHost))
                {
                    return personal;
                }
            }

            return await GetInternalAsync(repo, "Mail:");
        }
        catch (Exception ex)
        {
            // Mail yalnızca bildirim amaçlı: hata akışı bloklamaz ama SMTP yanlış
            // yapılandırma teşhisi için Serilog dosyasına iz bırakılır (DB'ye değil —
            // hata DB kaynaklıysa döngü oluşmasın)
            _logger.LogWarning(ex, "Mail ayarları okunamadı (GetAsync)");
            return null;
        }
    }

    public async Task<MailSettingsDto?> GetGlobalAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IApplicationSettingRepository>();
            return await GetInternalAsync(repo, "Mail:");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mail ayarları okunamadı (GetGlobalAsync)");
            return null;
        }
    }

    public async Task<MailSettingsDto?> GetPersonalOnlyAsync(Guid userId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IApplicationSettingRepository>();
            return await GetInternalAsync(repo, $"UserMail:{userId}:");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kişisel mail ayarları okunamadı (GetPersonalOnlyAsync)");
            return null;
        }
    }

    private async Task<MailSettingsDto?> GetInternalAsync(IApplicationSettingRepository repo, string prefix)
    {
        var settings = await repo.GetByPrefixAsync(prefix);
        if (settings.Count == 0) return null;

        var map = settings.ToDictionary(s => s.Key, s => s);

        bool passwordDecryptFailed = false;

        string? Get(string keySuffix)
        {
            string fullKey = prefix + keySuffix;
            if (!map.TryGetValue(fullKey, out var s) || s.Value is null) return null;
            if (!s.IsEncrypted) return s.Value;
            try   { return _protector.Unprotect(s.Value); }
            catch (Exception ex)
            {
                if (keySuffix == "Password")
                {
                    passwordDecryptFailed = true;
                    _logger.LogError(ex, "{Key} AES çözümü başarısız — anahtarı değişmiş olabilir", fullKey);
                    _ = _systemLog.LogWarningAsync("Mail", $"Mail şifresi AES çözümü başarısız ({fullKey}) — anahtar değişmiş olabilir, şifreyi tekrar kaydedin.", "MailSettingsService");
                }
                return null;
            }
        }

        return new MailSettingsDto
        {
            SmtpHost              = Get("SmtpHost")    ?? "",
            SmtpPort              = int.TryParse(Get("SmtpPort"),  out var port) ? port : 587,
            EnableSsl             = bool.TryParse(Get("EnableSsl"), out var ssl)  ? ssl  : true,
            SenderEmail           = Get("SenderEmail") ?? "",
            SenderName            = Get("SenderName")  ?? "",
            Username              = Get("Username")    ?? "",
            Password              = Get("Password")    ?? "",
            PasswordDecryptFailed = passwordDecryptFailed,
        };
    }
}

