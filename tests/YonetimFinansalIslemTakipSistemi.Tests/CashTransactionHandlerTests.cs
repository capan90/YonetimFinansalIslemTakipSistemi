using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.CreateCashTransaction;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.DeleteCashTransaction;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Nakit işlem handler'ları: yetki reddi, validasyon kuralları, soft delete ve audit.
/// Finansal çekirdeğin üretim güvencesi — Sprint 17'de eklendi.
/// </summary>
public class CashTransactionHandlerTests
{
    private static (CreateCashTransactionHandler Handler,
                    FakeCashTransactionRepository Repo,
                    FakeAuditLogService Audit,
                    FakeUserContext User) BuildCreate(bool grantPermission = true)
    {
        var repo  = new FakeCashTransactionRepository();
        var audit = new FakeAuditLogService();
        var user  = new FakeUserContext();
        if (grantPermission)
            user.SetUser(user.UserId, user.FullName,
                new HashSet<PermissionType> { PermissionType.CanCreateTransaction });

        var handler = new CreateCashTransactionHandler(repo, audit, user, new FakeTextNormalizationService());
        return (handler, repo, audit, user);
    }

    private static CreateCashTransactionRequest ValidRequest(Guid userId) => new()
    {
        TransactionDate = DateTime.Today,
        TransactionType = TransactionType.Giris,
        CurrencyType    = CurrencyType.TRY,
        Amount          = 100m,
        Description     = "Test tahsilatı",
        CreatedByUserId = userId
    };

    // ── Yetki ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_YetkisizKullanici_Reddedilir_KayitVeAuditYazilmaz()
    {
        var (handler, repo, audit, user) = BuildCreate(grantPermission: false);

        var result = await handler.HandleAsync(ValidRequest(user.UserId));

        Assert.False(result.Success);
        Assert.Empty(repo.Items);
        Assert.Empty(audit.Entries);
    }

    // ── Validasyon ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task Create_SifirVeyaNegatifTutar_Reddedilir(double amount)
    {
        var (handler, repo, _, user) = BuildCreate();

        var request = ValidRequest(user.UserId);
        request.Amount = (decimal)amount;

        var result = await handler.HandleAsync(request);

        Assert.False(result.Success);
        Assert.Empty(repo.Items);
    }

    [Fact]
    public async Task Create_IleriTarih_Reddedilir()
    {
        var (handler, _, _, user) = BuildCreate();

        var request = ValidRequest(user.UserId);
        request.TransactionDate = DateTime.Today.AddDays(1);

        var result = await handler.HandleAsync(request);

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_BosAciklama_Reddedilir(string description)
    {
        var (handler, _, _, user) = BuildCreate();

        var request = ValidRequest(user.UserId);
        request.Description = description;

        var result = await handler.HandleAsync(request);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Create_BosKullaniciId_Reddedilir()
    {
        var (handler, _, _, _) = BuildCreate();

        var result = await handler.HandleAsync(ValidRequest(Guid.Empty));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Create_GecerliIstek_KaydedilirVeAuditYazilir()
    {
        var (handler, repo, audit, user) = BuildCreate();

        var result = await handler.HandleAsync(ValidRequest(user.UserId));

        Assert.True(result.Success);
        var entity = Assert.Single(repo.Items);
        Assert.Equal(100m, entity.Amount);
        Assert.Equal(DateTimeKind.Utc, entity.TransactionDate.Kind); // Npgsql timestamptz kuralı
        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AuditAction.TransactionCreated, entry.Action);
    }

    // ── Silme ───────────────────────────────────────────────────────────────

    private static (DeleteCashTransactionHandler Handler,
                    FakeCashTransactionRepository Repo,
                    FakeAuditLogService Audit,
                    FakeUserContext User) BuildDelete(bool grantPermission = true)
    {
        var repo  = new FakeCashTransactionRepository();
        var audit = new FakeAuditLogService();
        var user  = new FakeUserContext();
        if (grantPermission)
            user.SetUser(user.UserId, user.FullName,
                new HashSet<PermissionType> { PermissionType.CanDeleteTransaction });

        return (new DeleteCashTransactionHandler(repo, audit, user), repo, audit, user);
    }

    private static CashTransaction SeedTransaction(FakeCashTransactionRepository repo)
    {
        var entity = new CashTransaction
        {
            Id              = Guid.NewGuid(),
            TransactionDate = DateTime.UtcNow.Date,
            TransactionType = TransactionType.Cikis,
            CurrencyType    = CurrencyType.USD,
            Amount          = 250m,
            Description     = "Silinecek kayıt"
        };
        repo.Items.Add(entity);
        return entity;
    }

    [Fact]
    public async Task Delete_YetkisizKullanici_Reddedilir_KayitSilinmez()
    {
        var (handler, repo, _, user) = BuildDelete(grantPermission: false);
        var entity = SeedTransaction(repo);

        var result = await handler.HandleAsync(new DeleteCashTransactionRequest
        {
            Id = entity.Id, DeletedByUserId = user.UserId
        });

        Assert.False(result.Success);
        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public async Task Delete_BosKullaniciId_Reddedilir()
    {
        var (handler, repo, _, _) = BuildDelete();
        var entity = SeedTransaction(repo);

        var result = await handler.HandleAsync(new DeleteCashTransactionRequest
        {
            Id = entity.Id, DeletedByUserId = Guid.Empty
        });

        Assert.False(result.Success);
        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public async Task Delete_GecerliIstek_SoftDeleteUygulanirVeEskiDegerlerAuditeYazilir()
    {
        var (handler, repo, audit, user) = BuildDelete();
        var entity = SeedTransaction(repo);

        var result = await handler.HandleAsync(new DeleteCashTransactionRequest
        {
            Id = entity.Id, DeletedByUserId = user.UserId
        });

        Assert.True(result.Success);
        Assert.True(entity.IsDeleted); // fiziksel silme yok
        Assert.Equal(user.UserId, entity.DeletedByUserId);
        Assert.Contains(repo.Items, x => x.Id == entity.Id);

        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AuditAction.TransactionDeleted, entry.Action);
        Assert.Contains("250", entry.OldValues); // eski değerler kaybolmaz
    }
}
