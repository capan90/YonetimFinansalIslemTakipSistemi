using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.Settings.UserPreferences;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Application.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Harf tercihinin oturum içinde anında uygulanmasını uçtan uca doğrular.
/// DI kayıtları App.xaml.cs ile birebir aynı yaşam sürelerini kullanır:
/// UserContext tek instance (IUserContext + IUserSession), normalizasyon Singleton,
/// handler'lar Scoped — gerçek uygulamadaki çözümleme zinciri simüle edilir.
/// </summary>
public class UserPreferenceSessionTests
{
    /// <summary>App.xaml.cs kayıtlarını birebir taklit eden container kurar.</summary>
    private static ServiceProvider BuildAppLikeProvider(
        UserContext userContext, IUserPreferenceRepository sharedRepo)
    {
        var services = new ServiceCollection();

        // App.xaml.cs ile aynı: tek UserContext örneği iki arayüz üzerinden
        services.AddSingleton(userContext);
        services.AddSingleton<IUserContext>(userContext);
        services.AddSingleton<IUserSession>(userContext);

        services.AddSingleton<IUserTextNormalizationService, UserTextNormalizationService>();
        services.AddSingleton(sharedRepo); // DB'yi temsil eden kalıcı store
        services.AddScoped<SaveUserPreferenceHandler>();
        services.AddScoped<GetUserPreferenceHandler>();
        services.AddSingleton<IAuditLogService>(new FakeAuditLogService());

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task TercihKaydedilince_AktifContext_AnindaGuncellenir()
    {
        var userContext = new UserContext();
        userContext.SetUser(Guid.NewGuid(), "Test", new HashSet<PermissionType>());
        using var provider = BuildAppLikeProvider(userContext, new FakeUserPreferenceRepository());

        using var settingsScope = provider.CreateScope(); // ayar penceresi scope'u
        var save = settingsScope.ServiceProvider.GetRequiredService<SaveUserPreferenceHandler>();
        var result = await save.HandleAsync(TextCasePreference.Uppercase);

        Assert.True(result.Success);
        Assert.Equal(TextCasePreference.Uppercase, userContext.TextCasePreference);
    }

    [Fact]
    public async Task TekrarLoginOlmadan_SonrakiNormalize_YeniTercihiKullanir()
    {
        var userContext = new UserContext();
        userContext.SetUser(Guid.NewGuid(), "Test", new HashSet<PermissionType>());
        using var provider = BuildAppLikeProvider(userContext, new FakeUserPreferenceRepository());

        // Tercih değişiklikleri sırayla: Olduğu Gibi → BÜYÜK → küçük → Olduğu Gibi
        var normalization = provider.GetRequiredService<IUserTextNormalizationService>();
        Assert.Equal("istanbul ödemesi", normalization.Normalize("istanbul ödemesi"));

        using (var scope = provider.CreateScope())
            await scope.ServiceProvider.GetRequiredService<SaveUserPreferenceHandler>()
                .HandleAsync(TextCasePreference.Uppercase);
        // Farklı scope'tan (yeni create işlemi) çözümlense de aynı tercih görülür
        using (var createScope = provider.CreateScope())
        {
            var n = createScope.ServiceProvider.GetRequiredService<IUserTextNormalizationService>();
            Assert.Equal("İSTANBUL ÖDEMESİ", n.Normalize("istanbul ödemesi"));
        }

        using (var scope = provider.CreateScope())
            await scope.ServiceProvider.GetRequiredService<SaveUserPreferenceHandler>()
                .HandleAsync(TextCasePreference.Lowercase);
        Assert.Equal("ışık plaka", normalization.Normalize("IŞIK PLAKA"));

        using (var scope = provider.CreateScope())
            await scope.ServiceProvider.GetRequiredService<SaveUserPreferenceHandler>()
                .HandleAsync(TextCasePreference.Preserve);
        Assert.Equal("Olduğu Gibi Metin", normalization.Normalize("Olduğu Gibi Metin"));
    }

    [Fact]
    public async Task UygulamaYenidenBaslatilinca_TercihDBdenYuklenir()
    {
        var userId = Guid.NewGuid();
        var sharedRepo = new FakeUserPreferenceRepository(); // "DB" — process'ler arası yaşar

        // İlk "process": kullanıcı tercih kaydeder
        var firstRun = new UserContext();
        firstRun.SetUser(userId, "Test", new HashSet<PermissionType>());
        using (var provider = BuildAppLikeProvider(firstRun, sharedRepo))
        using (var scope = provider.CreateScope())
            await scope.ServiceProvider.GetRequiredService<SaveUserPreferenceHandler>()
                .HandleAsync(TextCasePreference.Uppercase);

        // İkinci "process": yeni UserContext, LoginViewModel akışının aynısı
        var secondRun = new UserContext();
        using var provider2 = BuildAppLikeProvider(secondRun, sharedRepo);
        using var loginScope = provider2.CreateScope();

        secondRun.SetUser(userId, "Test", new HashSet<PermissionType>());
        var loaded = await loginScope.ServiceProvider
            .GetRequiredService<GetUserPreferenceHandler>().HandleAsync();
        ((IUserSession)secondRun).SetTextCasePreference(loaded);

        var normalization = provider2.GetRequiredService<IUserTextNormalizationService>();
        Assert.Equal("İSTANBUL", normalization.Normalize("istanbul"));
    }

    [Fact]
    public void SetUser_OturumIcindeCagrilinca_TercihiSifirlamaz()
    {
        // Regresyon: kullanıcı kendi izinlerini güncellediğinde SetUser yeniden çağrılır
        // (UserPermissionViewModel); bu çağrı harf tercihini kaybettirmemeli
        var userContext = new UserContext();
        var userId = Guid.NewGuid();
        userContext.SetUser(userId, "Test", new HashSet<PermissionType>());
        ((IUserSession)userContext).SetTextCasePreference(TextCasePreference.Uppercase);

        userContext.SetUser(userId, "Test", new HashSet<PermissionType> { PermissionType.CanViewReports });

        Assert.Equal(TextCasePreference.Uppercase, userContext.TextCasePreference);
    }

    [Fact]
    public async Task FarkliKullanicilarinTercihleri_Karismaz()
    {
        var sharedRepo = new FakeUserPreferenceRepository();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        // Kullanıcı A BÜYÜK HARF kaydeder
        var ctxA = new UserContext();
        ctxA.SetUser(userA, "A", new HashSet<PermissionType>());
        using (var providerA = BuildAppLikeProvider(ctxA, sharedRepo))
        using (var scope = providerA.CreateScope())
            await scope.ServiceProvider.GetRequiredService<SaveUserPreferenceHandler>()
                .HandleAsync(TextCasePreference.Uppercase);

        // Kullanıcı B giriş yapar: kendi kaydı yok → varsayılan Olduğu Gibi
        var ctxB = new UserContext();
        ctxB.SetUser(userB, "B", new HashSet<PermissionType>());
        using var providerB = BuildAppLikeProvider(ctxB, sharedRepo);
        using var loginScope = providerB.CreateScope();
        var loaded = await loginScope.ServiceProvider
            .GetRequiredService<GetUserPreferenceHandler>().HandleAsync();

        Assert.Equal(TextCasePreference.Preserve, loaded);
    }
}
