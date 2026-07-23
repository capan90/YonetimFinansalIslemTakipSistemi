using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Kimlik doğrulama akışı: BCrypt doğrulaması, jenerik hata mesajları,
/// başarısız denemelerin System Log'a yazılması ve başarılı giriş audit'i.
/// </summary>
public class AuthenticationServiceTests
{
    private const string CorrectPassword = "Dogru-Sifre-123";

    private static (DatabaseAuthenticationService Service,
                    FakeUserRepository Users,
                    FakeUserPermissionRepository Permissions,
                    FakeAuditLogService Audit,
                    FakeSystemLogService SystemLog) Build()
    {
        var users       = new FakeUserRepository();
        var permissions = new FakeUserPermissionRepository();
        var audit       = new FakeAuditLogService();
        var systemLog   = new FakeSystemLogService();
        var service     = new DatabaseAuthenticationService(users, permissions, audit, systemLog);
        return (service, users, permissions, audit, systemLog);
    }

    private static User SeedUser(FakeUserRepository users, bool isActive = true)
    {
        var user = new User
        {
            Id           = Guid.NewGuid(),
            FullName     = "Test Kullanıcı",
            UserName     = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(CorrectPassword),
            IsActive     = isActive
        };
        users.Items.Add(user);
        return user;
    }

    [Fact]
    public async Task BilinmeyenKullanici_JenerikMesajlaReddedilir_WarningLoglanir()
    {
        var (service, _, _, audit, systemLog) = Build();

        var result = await service.AuthenticateAsync("yok-boyle-biri", "herhangi");

        Assert.False(result.Success);
        // Güvenlik: "kullanıcı bulunamadı" gibi ayrıntı sızdırılmaz
        Assert.Equal("Kullanıcı adı veya şifre hatalı.", result.ErrorMessage);
        Assert.Empty(audit.Entries);
        Assert.Contains(systemLog.Entries, e => e.Level == "Warning" && e.Category == "Auth");
    }

    [Fact]
    public async Task PasifHesap_Reddedilir_WarningLoglanir()
    {
        var (service, users, _, _, systemLog) = Build();
        SeedUser(users, isActive: false);

        var result = await service.AuthenticateAsync("testuser", CorrectPassword);

        Assert.False(result.Success);
        Assert.Contains(systemLog.Entries, e => e.Level == "Warning");
    }

    [Fact]
    public async Task HataliSifre_JenerikMesajlaReddedilir_AuditYazilmaz()
    {
        var (service, users, _, audit, systemLog) = Build();
        SeedUser(users);

        var result = await service.AuthenticateAsync("testuser", "yanlis-sifre");

        Assert.False(result.Success);
        Assert.Equal("Kullanıcı adı veya şifre hatalı.", result.ErrorMessage);
        Assert.Empty(audit.Entries);
        // Şifre hiçbir log kaydında yer almamalı
        Assert.DoesNotContain(systemLog.Entries, e => e.Message.Contains("yanlis-sifre"));
    }

    [Fact]
    public async Task DogruSifre_IzinlerYuklenir_LoginAuditYazilir()
    {
        var (service, users, permissions, audit, _) = Build();
        var user = SeedUser(users);
        permissions.Map[user.Id] = [PermissionType.CanCreateTransaction, PermissionType.CanViewReports];

        var result = await service.AuthenticateAsync("testuser", CorrectPassword);

        Assert.True(result.Success);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(2, result.Permissions.Count);
        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AuditAction.UserLoggedIn, entry.Action);
    }
}
