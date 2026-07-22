using YonetimFinansalIslemTakipSistemi.Application.Features.Settings.UserPreferences;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Harf duyarlılığı kişisel bir kullanıcı ayarıdır: CanAccessSettings dahil hiçbir
/// sistem ayarı yetkisi gerektirmez. Giriş yapmış her kullanıcı kendi tercihini
/// görüntüleyip değiştirebilir.
/// </summary>
public class UserPreferenceAccessTests
{
    [Fact]
    public async Task YetkisizKullanici_KendiHarfTercihiniKaydedebilir()
    {
        var repo = new FakeUserPreferenceRepository();
        var user = new FakeUserContext(); // hiçbir permission verilmedi — CanAccessSettings=false
        Assert.False(user.HasPermission(PermissionType.CanAccessSettings));

        var save = new SaveUserPreferenceHandler(repo, new FakeAuditLogService(), user, user);
        var result = await save.HandleAsync(TextCasePreference.Uppercase);

        Assert.True(result.Success);
        Assert.Equal(TextCasePreference.Uppercase, repo.Items.Single().TextCase);
        // Tercih oturuma anında uygulanır
        Assert.Equal(TextCasePreference.Uppercase, user.TextCasePreference);
    }

    [Fact]
    public async Task YetkisizKullanici_KendiTercihiniOkuyabilir()
    {
        var repo = new FakeUserPreferenceRepository();
        var user = new FakeUserContext();

        var save = new SaveUserPreferenceHandler(repo, new FakeAuditLogService(), user, user);
        await save.HandleAsync(TextCasePreference.Lowercase);

        var get = new GetUserPreferenceHandler(repo, user);
        Assert.Equal(TextCasePreference.Lowercase, await get.HandleAsync());
    }

    [Fact]
    public async Task TercihKaydi_AuditeYazilir()
    {
        var repo  = new FakeUserPreferenceRepository();
        var audit = new FakeAuditLogService();
        var user  = new FakeUserContext();

        var save = new SaveUserPreferenceHandler(repo, audit, user, user);
        await save.HandleAsync(TextCasePreference.Uppercase);

        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AuditAction.UserPreferenceUpdated, entry.Action);
        Assert.Contains("BÜYÜK HARF", entry.NewValues);
    }

    [Fact]
    public async Task SadeceKargoYetkiliKullanici_TercihKaydeder_VeKargoKaydindaUygulanir()
    {
        // Yalnızca kargo yetkisi olan kullanıcı (CanAccessSettings yok, finans ekranı yok):
        // Kargo ana ekranındaki Yardım → Kullanıcı Ayarlarım girişi aynı handler'ı kullanır
        var user = new FakeUserContext();
        user.SetUser(Guid.NewGuid(), "Kargo Kullanıcısı", new HashSet<PermissionType>
        {
            PermissionType.CanViewCargoModule,
            PermissionType.CanManageOutgoingCargo
        });
        Assert.False(user.HasPermission(PermissionType.CanAccessSettings));

        var save = new SaveUserPreferenceHandler(
            new FakeUserPreferenceRepository(), new FakeAuditLogService(), user, user);
        var saved = await save.HandleAsync(TextCasePreference.Uppercase);
        Assert.True(saved.Success);

        // Tercih anında aktif — sonraki kargo kaydında açıklama büyük harfe dönüşür
        var repo = new FakeCargoShipmentRepository();
        var create = new Application.Features.CargoShipment.Commands.CreateCargoShipment.CreateCargoShipmentHandler(
            repo, new FakeAuditLogService(), user, new FakeCargoDashboardCache(),
            new Application.Services.UserTextNormalizationService(user), new FakeSystemLogService());

        var result = await create.HandleAsync(
            new Application.Features.CargoShipment.Commands.CreateCargoShipment.CreateCargoShipmentRequest
            {
                Direction    = CargoShipmentDirection.Outgoing,
                ShipmentDate = DateTime.Today,
                Status       = CargoShipmentStatus.Prepared,
                Notes        = "acil gönderi istanbul"
            });

        Assert.True(result.Success);
        Assert.Equal("ACİL GÖNDERİ İSTANBUL", repo.Added.Single().Notes);
    }

    [Fact]
    public async Task FarkliKullanicilar_AyniAltyapiylaFarkliTercihKaydeder()
    {
        var repo = new FakeUserPreferenceRepository();

        var userA = new FakeUserContext();
        userA.GrantAll(); // yetkili kullanıcı (Ayarlar menüsünden gelir)
        var userB = new FakeUserContext(); // yetkisiz kullanıcı (Yardım menüsünden gelir)

        await new SaveUserPreferenceHandler(repo, new FakeAuditLogService(), userA, userA)
            .HandleAsync(TextCasePreference.Uppercase);
        await new SaveUserPreferenceHandler(repo, new FakeAuditLogService(), userB, userB)
            .HandleAsync(TextCasePreference.Lowercase);

        Assert.Equal(TextCasePreference.Uppercase,
            repo.Items.Single(x => x.UserId == userA.UserId).TextCase);
        Assert.Equal(TextCasePreference.Lowercase,
            repo.Items.Single(x => x.UserId == userB.UserId).TextCase);
    }
}
