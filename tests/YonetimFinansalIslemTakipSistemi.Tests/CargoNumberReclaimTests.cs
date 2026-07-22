using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.CreateCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.DeleteCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Kontrollü numara geri alma iş kuralı: yalnızca yönün EN SON numarası silinirse
/// numara sonraki kayıtta yeniden kullanılır; aradaki silinmiş numaralar asla geri dönmez.
/// Gerçek PostgreSQL'de aynı garanti tek transaction + koşullu UPDATE satır kilidiyle sağlanır;
/// burada fake repository aynı algoritmayı tek kilit altında uygular.
/// </summary>
public class CargoNumberReclaimTests
{
    private static (CreateCargoShipmentHandler Create, DeleteCargoShipmentHandler Delete,
                    FakeCargoShipmentRepository Repo, FakeAuditLogService Audit)
        CreateHandlers()
    {
        var repo  = new FakeCargoShipmentRepository();
        var audit = new FakeAuditLogService();
        var user  = new FakeUserContext();
        user.GrantAll();

        var create = new CreateCargoShipmentHandler(
            repo, new FakeCargoCompanyRepository(), audit, user,
            new FakeCargoDashboardCache(), new UserTextNormalizationService(user));
        var delete = new DeleteCargoShipmentHandler(repo, audit, user, new FakeCargoDashboardCache());
        return (create, delete, repo, audit);
    }

    private static CreateCargoShipmentRequest NewRequest(CargoShipmentDirection direction) => new()
    {
        Direction    = direction,
        ShipmentDate = DateTime.Today,
        Status       = CargoShipmentStatus.Prepared
    };

    private static DeleteCargoShipmentRequest DeleteRequest(CargoShipment entity) => new()
    {
        Id              = entity.Id,
        Direction       = entity.Direction,
        DeletedByUserId = Guid.NewGuid()
    };

    [Fact]
    public async Task SonNumaraSilinirse_TekrarKullanilir()
    {
        var (create, delete, repo, _) = CreateHandlers();

        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing)); // GDN00001
        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing)); // GDN00002
        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing)); // GDN00003

        var last = repo.Added.Single(x => x.ShipmentNumber == "GDN00003");
        var result = await delete.HandleAsync(DeleteRequest(last));
        Assert.True(result.Success);

        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing));
        Assert.Contains(repo.Added, x => x.ShipmentNumber == "GDN00003" && !x.IsDeleted);
    }

    [Fact]
    public async Task AradakiNumaraSilinirse_TekrarKullanilmaz()
    {
        var (create, delete, repo, _) = CreateHandlers();

        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing)); // GDN00001
        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing)); // GDN00002
        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing)); // GDN00003

        var middle = repo.Added.Single(x => x.ShipmentNumber == "GDN00002");
        var result = await delete.HandleAsync(DeleteRequest(middle));
        Assert.True(result.Success);

        Assert.Equal(3, repo.GetCounter(CargoShipmentDirection.Outgoing)); // sayaç geri alınmadı

        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing));
        Assert.Contains(repo.Added, x => x.ShipmentNumber == "GDN00004");
        Assert.DoesNotContain(repo.Added, x => x.ShipmentNumber == "GDN00002" && !x.IsDeleted);
    }

    [Fact]
    public async Task GelenSilme_GidenSayaciniEtkilemez()
    {
        var (create, delete, repo, _) = CreateHandlers();

        await create.HandleAsync(NewRequest(CargoShipmentDirection.Incoming)); // GLN00001
        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing)); // GDN00001

        var incoming = repo.Added.Single(x => x.Direction == CargoShipmentDirection.Incoming);
        await delete.HandleAsync(DeleteRequest(incoming)); // GLN00001 geri alınır

        Assert.Equal(0, repo.GetCounter(CargoShipmentDirection.Incoming));
        Assert.Equal(1, repo.GetCounter(CargoShipmentDirection.Outgoing)); // giden etkilenmedi

        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing));
        Assert.Contains(repo.Added, x => x.ShipmentNumber == "GDN00002");
    }

    [Fact]
    public async Task GidenSilme_GelenSayaciniEtkilemez()
    {
        var (create, delete, repo, _) = CreateHandlers();

        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing)); // GDN00001
        await create.HandleAsync(NewRequest(CargoShipmentDirection.Incoming)); // GLN00001

        var outgoing = repo.Added.Single(x => x.Direction == CargoShipmentDirection.Outgoing);
        await delete.HandleAsync(DeleteRequest(outgoing));

        Assert.Equal(1, repo.GetCounter(CargoShipmentDirection.Incoming)); // gelen etkilenmedi

        await create.HandleAsync(NewRequest(CargoShipmentDirection.Incoming));
        Assert.Contains(repo.Added, x => x.ShipmentNumber == "GLN00002");
    }

    [Fact]
    public async Task SilmeRollbackOlursa_SayacDegismez()
    {
        var (create, _, repo, _) = CreateHandlers();

        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing)); // GDN00001
        var entity = repo.Added.Single();

        repo.FailNextDelete = true; // transaction rollback simülasyonu
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repo.SoftDeleteWithNumberReclaimAsync(entity));

        Assert.Equal(1, repo.GetCounter(CargoShipmentDirection.Outgoing)); // sayaç değişmedi
        Assert.Equal("GDN00001", entity.ShipmentNumber);                   // numara korundu
        Assert.False(entity.IsDeleted);

        // Rollback sonrası oluşturma kaldığı yerden devam eder
        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing));
        Assert.Contains(repo.Added, x => x.ShipmentNumber == "GDN00002");
    }

    [Fact]
    public async Task DahaYuksekKayitVarsa_SayacGeriAlinmaz()
    {
        var (create, delete, repo, _) = CreateHandlers();

        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing)); // GDN00001
        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing)); // GDN00002
        var last = repo.Added.Single(x => x.ShipmentNumber == "GDN00002");

        // Son numara silinip geri alındı (sayaç 1'e döndü), ardından yeni kayıt GDN00002'yi aldı
        await delete.HandleAsync(DeleteRequest(last));
        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing)); // GDN00002 (yeniden)

        // Şimdi GDN00001 silinirse: sayaç (2) kayıt numarasına (1) eşit değil → geri alma yok
        var first = repo.Added.Single(x => x.ShipmentNumber == "GDN00001");
        await delete.HandleAsync(DeleteRequest(first));

        Assert.Equal(2, repo.GetCounter(CargoShipmentDirection.Outgoing));

        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing));
        Assert.Contains(repo.Added, x => x.ShipmentNumber == "GDN00003");
        Assert.DoesNotContain(repo.Added, x => x.ShipmentNumber == "GDN00001" && !x.IsDeleted);
    }

    [Fact]
    public async Task EskiFormatNumaraSilinirse_SayacaDokunulmaz_NumaraKorunur()
    {
        var (create, delete, repo, _) = CreateHandlers();

        // Eski format kayıt (migration öncesi dönemden)
        var legacy = new CargoShipment
        {
            Id             = Guid.NewGuid(),
            ShipmentNumber = "G-2026-0007",
            Direction      = CargoShipmentDirection.Outgoing,
            ShipmentDate   = DateTime.Today
        };
        repo.Existing.Add(legacy);

        await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing)); // GDN00001

        await delete.HandleAsync(DeleteRequest(legacy));

        Assert.Equal("G-2026-0007", legacy.ShipmentNumber); // eski numaraya dokunulmaz
        Assert.True(legacy.IsDeleted);
        Assert.Equal(1, repo.GetCounter(CargoShipmentDirection.Outgoing)); // sayaç değişmedi
    }

    [Fact]
    public async Task EszamanliCreateVeDelete_DuplicateNumaraUretmez()
    {
        var (create, delete, repo, _) = CreateHandlers();

        // Başlangıç havuzu
        for (var i = 0; i < 5; i++)
            await create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing));

        // Paralel: 20 create + son kayıtları silme denemeleri (sayaç kilidi serileştirir)
        var deletions = repo.Added.OrderByDescending(x => x.ShipmentNumber).Take(3)
            .Select(e => Task.Run(() => delete.HandleAsync(DeleteRequest(e))));
        var creations = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => create.HandleAsync(NewRequest(CargoShipmentDirection.Outgoing))));

        await Task.WhenAll(deletions.Concat<Task>(creations));

        // Aktif kayıtlar arasında duplicate numara olamaz
        var activeNumbers = repo.Added.Where(x => !x.IsDeleted && x.ShipmentNumber != null)
            .Select(x => x.ShipmentNumber!).ToList();
        Assert.Equal(activeNumbers.Count, activeNumbers.Distinct().Count());
    }
}
