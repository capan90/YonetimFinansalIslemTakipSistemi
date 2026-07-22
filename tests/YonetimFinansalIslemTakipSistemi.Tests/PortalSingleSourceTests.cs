using YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Queries.GetCargoCompanyList;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.CreateCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.UpdateCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Label;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;
using YonetimFinansalIslemTakipSistemi.Application.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Portal/takip bağlantısında çalışma zamanı tek kaynağı CargoCompany.PortalUrl'dir.
/// Eski kayıtlarda saklı kayıt-bazlı TrackingUrl değerleri korunur ve öncelik alır;
/// yeni kayıtlar yalnızca firma PortalUrl'ini kullanır. Firma adına hard-code yoktur.
/// </summary>
public class PortalSingleSourceTests
{
    private const string PortalUrl = "https://selfservis.yurticikargo.com/Login.aspx?ReturnUrl=%2fMain.aspx";

    private static CargoCompany NewCompany(string? portalUrl, string? template = null) => new()
    {
        Id                  = Guid.NewGuid(),
        Name                = "Yurtiçi Kargo",
        PortalUrl           = portalUrl,
        TrackingUrlTemplate = template,
        IsActive            = true
    };

    [Fact]
    public async Task YeniKargoKaydi_SablondanTrackingUrlUretmez()
    {
        // Eski davranış: şablon + takip no'dan kayıt-bazlı URL üretilirdi.
        // Yeni davranış: kayıt TrackingUrl'süz oluşur; bağlantı firma PortalUrl'inden gelir.
        var repo = new FakeCargoShipmentRepository();
        var user = new FakeUserContext();
        user.GrantAll();
        var handler = new CreateCargoShipmentHandler(
            repo, new FakeAuditLogService(), user,
            new FakeCargoDashboardCache(), new UserTextNormalizationService(user),
            new FakeSystemLogService());

        var company = NewCompany(PortalUrl, template: "https://eski-sablon.com/{0}");
        var result = await handler.HandleAsync(new CreateCargoShipmentRequest
        {
            Direction      = CargoShipmentDirection.Outgoing,
            ShipmentDate   = DateTime.Today,
            Status         = CargoShipmentStatus.Prepared,
            CargoCompanyId = company.Id,
            TrackingNumber = "TK-1234"
        });

        Assert.True(result.Success);
        Assert.Null(repo.Added.Single().TrackingUrl);
    }

    [Fact]
    public async Task Guncelleme_EskiKayittakiTrackingUrluKorur()
    {
        var repo = new FakeCargoShipmentRepository();
        var user = new FakeUserContext();
        user.GrantAll();
        var legacy = new CargoShipment
        {
            Id           = Guid.NewGuid(),
            Direction    = CargoShipmentDirection.Outgoing,
            Status       = CargoShipmentStatus.Prepared,
            TrackingUrl  = "https://eski-takip.com/ABC123"
        };
        repo.Existing.Add(legacy);

        var handler = new UpdateCargoShipmentHandler(
            repo, new FakeAuditLogService(), user,
            new FakeCargoDashboardCache(), new UserTextNormalizationService(user));

        var result = await handler.HandleAsync(new UpdateCargoShipmentRequest
        {
            Id           = legacy.Id,
            Direction    = CargoShipmentDirection.Outgoing,
            ShipmentDate = DateTime.Today,
            Status       = CargoShipmentStatus.HandedToCargo
        });

        Assert.True(result.Success);
        Assert.Equal("https://eski-takip.com/ABC123", legacy.TrackingUrl); // veri kaybı yok
    }

    [Fact]
    public async Task ListeDto_FirmaPortalUrlineDusulur_EskiKayitOncelikli()
    {
        var repo = new FakeCargoShipmentRepository();
        var user = new FakeUserContext();
        user.GrantAll();
        var company = NewCompany(PortalUrl);

        repo.Existing.Add(new CargoShipment
        {
            Id = Guid.NewGuid(), Direction = CargoShipmentDirection.Outgoing,
            CargoCompany = company, CargoCompanyId = company.Id,
            TrackingUrl = null // yeni kayıt
        });
        repo.Existing.Add(new CargoShipment
        {
            Id = Guid.NewGuid(), Direction = CargoShipmentDirection.Outgoing,
            CargoCompany = company, CargoCompanyId = company.Id,
            TrackingUrl = "https://eski-takip.com/XYZ" // eski kayıt
        });
        repo.Existing.Add(new CargoShipment
        {
            Id = Guid.NewGuid(), Direction = CargoShipmentDirection.Outgoing // firmasız kayıt
        });

        var handler = new GetCargoShipmentListHandler(repo, user);
        var list = await handler.HandleAsync(new GetCargoShipmentListQuery
        {
            Direction = CargoShipmentDirection.Outgoing
        });

        Assert.Contains(list, x => x.TrackingUrl == PortalUrl);                    // portal fallback
        Assert.Contains(list, x => x.TrackingUrl == "https://eski-takip.com/XYZ"); // eski değer öncelikli
        Assert.Contains(list, x => string.IsNullOrEmpty(x.TrackingUrl));           // URL yok → buton pasif
    }

    [Fact]
    public void BildirimVeEtiket_PortalUrlFallbackKullanir()
    {
        var company = NewCompany(PortalUrl);
        var shipment = new CargoShipment
        {
            Id = Guid.NewGuid(),
            Direction    = CargoShipmentDirection.Outgoing,
            CargoCompany = company,
            TrackingUrl  = null
        };

        Assert.Equal(PortalUrl, CargoNotificationBuilder.Build(shipment).TrackingUrl);
        Assert.Equal(PortalUrl, CargoLabelBuilder.Build(shipment).TrackingUrl);

        // Eski kayıt: saklı değer öncelikli
        shipment.TrackingUrl = "https://eski-takip.com/XYZ";
        Assert.Equal("https://eski-takip.com/XYZ", CargoNotificationBuilder.Build(shipment).TrackingUrl);
    }

    [Fact]
    public async Task PortalUrl_YonetimYetkisiOlmadan_GoruntulemeYetkisiyleGelir()
    {
        // Portal bağlantısını AÇMAK için Kargo Firmaları yönetim yetkisi gerekmez;
        // kargo modülü görüntüleme yetkisi yeterlidir
        var repo = new FakeCargoCompanyRepository();
        repo.Items.Add(NewCompany(PortalUrl));

        var user = new FakeUserContext();
        user.SetUser(Guid.NewGuid(), "Kargo Kullanıcısı",
            new HashSet<PermissionType> { PermissionType.CanViewCargoModule });
        Assert.False(user.HasPermission(PermissionType.CanManageCargoCompanies));

        var handler = new GetCargoCompanyListHandler(repo, user);
        var list = await handler.HandleAsync(new GetCargoCompanyListQuery { IsActive = true });

        Assert.Equal(PortalUrl, list.Single().PortalUrl);
    }
}
