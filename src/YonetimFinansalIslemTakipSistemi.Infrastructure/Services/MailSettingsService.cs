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
            return await GetInternalAsync();
        }
        catch
        {
            // Tablo henüz oluşturulmamış (migration bekleniyor) ya da beklenmedik hata — null dönder
            return null;
        }
    }

    private async Task<MailSettingsDto?> GetInternalAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IApplicationSettingRepository>();

        var settings = await repo.GetByPrefixAsync("Mail:");
        if (settings.Count == 0) return null;

        var map = settings.ToDictionary(s => s.Key, s => s);

        bool passwordDecryptFailed = false;

        string? Get(string key)
        {
            if (!map.TryGetValue(key, out var s) || s.Value is null) return null;
            if (!s.IsEncrypted) return s.Value;
            try   { return _protector.Unprotect(s.Value); }
            catch (Exception ex)
            {
                if (key == "Mail:Password")
                {
                    passwordDecryptFailed = true;
                    // AES anahtarı değişmişse eski şifre çözülemez; kullanıcı tekrar kaydetmeli
                    _logger.LogError(ex, "Mail:Password AES çözümü başarısız — anahtarı değişmiş olabilir");
                    _ = _systemLog.LogWarningAsync("Mail", "Mail şifresi AES çözümü başarısız — anahtar değişmiş olabilir, şifreyi tekrar kaydedin.", "MailSettingsService");
                }
                return null;
            }
        }

        return new MailSettingsDto
        {
            SmtpHost              = Get("Mail:SmtpHost")    ?? "",
            SmtpPort              = int.TryParse(Get("Mail:SmtpPort"),  out var port) ? port : 587,
            EnableSsl             = bool.TryParse(Get("Mail:EnableSsl"), out var ssl)  ? ssl  : true,
            SenderEmail           = Get("Mail:SenderEmail") ?? "",
            SenderName            = Get("Mail:SenderName")  ?? "",
            Username              = Get("Mail:Username")    ?? "",
            Password              = Get("Mail:Password")    ?? "",
            PasswordDecryptFailed = passwordDecryptFailed,
        };
    }
}

