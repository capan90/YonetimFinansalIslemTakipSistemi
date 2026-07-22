using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.CreateCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.UpdateCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Otomatik kargo numarası davranışları. Gerçek DB kilidi (UPDATE ... RETURNING)
/// yerine atomik sayaçlı fake repository ile handler sözleşmesi doğrulanır;
/// unique index PostgreSQL migration'ında ayrıca mevcuttur.
/// </summary>
public class CargoShipmentNumberTests
{
    private static (CreateCargoShipmentHandler Handler, FakeCargoShipmentRepository Repo, FakeUserContext User)
        CreateHandler()
    {
        var repo = new FakeCargoShipmentRepository();
        var user = new FakeUserContext();
        user.GrantAll();
        var handler = new CreateCargoShipmentHandler(
            repo,
            new FakeCargoCompanyRepository(),
            new FakeAuditLogService(),
            user,
            new FakeCargoDashboardCache(),
            new UserTextNormalizationService(user));
        return (handler, repo, user);
    }

    private static CreateCargoShipmentRequest NewRequest(CargoShipmentDirection direction) => new()
    {
        Direction    = direction,
        ShipmentDate = DateTime.Today,
        Status       = CargoShipmentStatus.Prepared
    };

    [Fact]
    public async Task IlkGelenKargo_GLN00001_Alir()
    {
        var (handler, repo, _) = CreateHandler();
        var result = await handler.HandleAsync(NewRequest(CargoShipmentDirection.Incoming));

        Assert.True(result.Success);
        Assert.Equal("GLN00001", repo.Added.Single().ShipmentNumber);
    }

    [Fact]
    public async Task IlkGidenKargo_GDN00001_Alir()
    {
        var (handler, repo, _) = CreateHandler();
        var result = await handler.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing));

        Assert.True(result.Success);
        Assert.Equal("GDN00001", repo.Added.Single().ShipmentNumber);
    }

    [Fact]
    public async Task GelenVeGiden_BagimsizSayacKullanir()
    {
        var (handler, repo, _) = CreateHandler();

        await handler.HandleAsync(NewRequest(CargoShipmentDirection.Incoming));
        await handler.HandleAsync(NewRequest(CargoShipmentDirection.Incoming));
        await handler.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing));

        var numbers = repo.Added.Select(x => x.ShipmentNumber).ToList();
        Assert.Contains("GLN00001", numbers);
        Assert.Contains("GLN00002", numbers);
        Assert.Contains("GDN00001", numbers);
    }

    [Fact]
    public async Task AradakiSoftDeleteEdilenNumara_TekrarKullanilmaz()
    {
        var (handler, repo, _) = CreateHandler();

        await handler.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing)); // GDN00001
        await handler.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing)); // GDN00002
        var first = repo.Added.Single(x => x.ShipmentNumber == "GDN00001");

        // Aradaki numara silinir — sayaç geri alınmaz (yalnızca SON numara geri alınabilir)
        await repo.SoftDeleteWithNumberReclaimAsync(first);

        await handler.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing));
        Assert.Contains(repo.Added, x => x.ShipmentNumber == "GDN00003");
        Assert.DoesNotContain(repo.Added, x => x.ShipmentNumber == "GDN00001" && !x.IsDeleted);
    }

    [Fact]
    public async Task EszamanliOlusturmalar_MukerrerNumaraUretmez()
    {
        var (handler, repo, _) = CreateHandler();

        // 50 paralel oluşturma — unique simülasyonu duplicate'te exception fırlatır
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => handler.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing)));
        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
        var numbers = repo.Added.Select(x => x.ShipmentNumber).ToList();
        Assert.Equal(50, numbers.Count);
        Assert.Equal(50, numbers.Distinct().Count());
    }

    [Fact]
    public async Task Guncelleme_NumarayiDegistiremez()
    {
        var repo = new FakeCargoShipmentRepository();
        var user = new FakeUserContext();
        user.GrantAll();
        var entity = new CargoShipment
        {
            Id             = Guid.NewGuid(),
            ShipmentNumber = "GDN00001",
            Direction      = CargoShipmentDirection.Outgoing,
            Status         = CargoShipmentStatus.Prepared
        };
        repo.Existing.Add(entity);

        var handler = new UpdateCargoShipmentHandler(
            repo,
            new FakeCargoCompanyRepository(),
            new FakeAuditLogService(),
            user,
            new FakeCargoDashboardCache(),
            new UserTextNormalizationService(user));

        // UpdateCargoShipmentRequest'te ShipmentNumber alanı yoktur (derleme garantisi);
        // handler mevcut numarayı korur
        var result = await handler.HandleAsync(new UpdateCargoShipmentRequest
        {
            Id           = entity.Id,
            Direction    = CargoShipmentDirection.Outgoing,
            ShipmentDate = DateTime.Today,
            Status       = CargoShipmentStatus.HandedToCargo,
            SenderName   = "Yeni Gönderen"
        });

        Assert.True(result.Success);
        Assert.Equal("GDN00001", entity.ShipmentNumber);
    }
}
