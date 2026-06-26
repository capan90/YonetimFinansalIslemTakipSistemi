using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISecretProtector     _protector;

    public MailSettingsService(IServiceScopeFactory scopeFactory, ISecretProtector protector)
    {
        _scopeFactory = scopeFactory;
        _protector    = protector;
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

        string? Get(string key)
        {
            if (!map.TryGetValue(key, out var s) || s.Value is null) return null;
            if (!s.IsEncrypted) return s.Value;
            try   { return _protector.Unprotect(s.Value); }
            catch { return null; }
        }

        return new MailSettingsDto
        {
            SmtpHost    = Get("Mail:SmtpHost")    ?? "",
            SmtpPort    = int.TryParse(Get("Mail:SmtpPort"),  out var port) ? port : 587,
            EnableSsl   = bool.TryParse(Get("Mail:EnableSsl"), out var ssl)  ? ssl  : true,
            SenderEmail = Get("Mail:SenderEmail") ?? "",
            SenderName  = Get("Mail:SenderName")  ?? "",
            Username    = Get("Mail:Username")    ?? "",
            Password    = Get("Mail:Password")    ?? "",
        };
    }
}

