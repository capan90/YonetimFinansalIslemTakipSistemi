using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// Uygulama ayarları tablosunda hiç mail ayarı yoksa,
/// appsettings.json'daki mevcut SMTP bilgilerini otomatik olarak ekler.
/// Bu sayede mevcut kurulumlar ekstra adım gerekmeden çalışmaya devam eder.
/// </summary>
public class MailSettingsSeeder
{
    private readonly IApplicationSettingRepository _repo;
    private readonly ISecretProtector              _protector;

    public MailSettingsSeeder(IApplicationSettingRepository repo, ISecretProtector protector)
    {
        _repo      = repo;
        _protector = protector;
    }

    public async Task SeedAsync(SmtpNotificationOptions smtp)
    {
        // Zaten ayar varsa bir şey yapma
        var existing = await _repo.GetByKeyAsync("Mail:SmtpHost");
        if (existing is not null) return;

        if (string.IsNullOrWhiteSpace(smtp.SmtpHost)) return;

        // Sistem tarafından oluşturulan kayıtlar Guid.Empty userId ile işaretlenir
        var systemId = Guid.Empty;

        await _repo.UpsertAsync("Mail:SmtpHost",    smtp.SmtpHost,                         false, systemId);
        await _repo.UpsertAsync("Mail:SmtpPort",    smtp.SmtpPort.ToString(),               false, systemId);
        await _repo.UpsertAsync("Mail:EnableSsl",   smtp.SmtpEnableSsl.ToString(),          false, systemId);
        await _repo.UpsertAsync("Mail:SenderEmail", smtp.From,                              false, systemId);
        await _repo.UpsertAsync("Mail:SenderName",  "Yönetim Sistemi",                     false, systemId);
        await _repo.UpsertAsync("Mail:Username",    smtp.SmtpUsername,                      false, systemId);

        if (!string.IsNullOrWhiteSpace(smtp.SmtpPassword))
        {
            var encrypted = _protector.Protect(smtp.SmtpPassword);
            await _repo.UpsertAsync("Mail:Password", encrypted, true, systemId);
        }
    }
}
