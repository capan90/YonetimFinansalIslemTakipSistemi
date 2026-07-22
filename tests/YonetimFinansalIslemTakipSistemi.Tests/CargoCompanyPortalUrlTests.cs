using YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Commands.UpdateCargoCompany;
using YonetimFinansalIslemTakipSistemi.Application.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Tests;

public class CargoCompanyPortalUrlTests
{
    private static (UpdateCargoCompanyHandler Handler, FakeCargoCompanyRepository Repo, CargoCompany Entity)
        CreateHandler()
    {
        var repo = new FakeCargoCompanyRepository();
        var entity = new CargoCompany { Id = Guid.NewGuid(), Name = "Yurtiçi Kargo" };
        repo.Items.Add(entity);

        var user = new FakeUserContext();
        user.GrantAll();
        var handler = new UpdateCargoCompanyHandler(
            repo, new FakeAuditLogService(), user, new UserTextNormalizationService(user));
        return (handler, repo, entity);
    }

    [Fact]
    public async Task GecerliPortalUrl_Kaydedilir()
    {
        var (handler, _, entity) = CreateHandler();
        var url = "https://selfservis.yurticikargo.com/Login.aspx?ReturnUrl=%2fMain.aspx";

        var result = await handler.HandleAsync(new UpdateCargoCompanyRequest
        {
            Id = entity.Id, Name = entity.Name, PortalUrl = url, IsActive = true
        });

        Assert.True(result.Success);
        Assert.Equal(url, entity.PortalUrl);
    }

    [Fact]
    public async Task GecersizPortalUrl_Reddedilir()
    {
        var (handler, _, entity) = CreateHandler();

        var result = await handler.HandleAsync(new UpdateCargoCompanyRequest
        {
            Id = entity.Id, Name = entity.Name, PortalUrl = "ftp://gecersiz", IsActive = true
        });

        Assert.False(result.Success);
        Assert.Contains("http", result.ErrorMessage);
    }

    [Fact]
    public async Task BosPortalUrl_KabulEdilir()
    {
        var (handler, _, entity) = CreateHandler();

        var result = await handler.HandleAsync(new UpdateCargoCompanyRequest
        {
            Id = entity.Id, Name = entity.Name, PortalUrl = null, IsActive = true
        });

        Assert.True(result.Success);
        Assert.Null(entity.PortalUrl);
    }

    [Fact]
    public async Task PortalUrl_FirmaAdinaBagliDegildir()
    {
        // Firma adı değişse bile bağlantı entity üzerinden çalışmaya devam eder
        var (handler, _, entity) = CreateHandler();
        var url = "https://portal.ornek-kargo.com";

        var result = await handler.HandleAsync(new UpdateCargoCompanyRequest
        {
            Id = entity.Id, Name = "Yepyeni Firma Adı", PortalUrl = url, IsActive = true
        });

        Assert.True(result.Success);
        Assert.Equal("Yepyeni Firma Adı", entity.Name);
        Assert.Equal(url, entity.PortalUrl);
    }
}
