using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.CreateWhatsAppContact;
using YonetimFinansalIslemTakipSistemi.Application.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

public class WhatsAppContactTests
{
    private static (CreateWhatsAppContactHandler Handler, FakeWhatsAppContactRepository Repo, FakeUserContext User)
        CreateHandler(TextCasePreference preference = TextCasePreference.Preserve)
    {
        var repo = new FakeWhatsAppContactRepository();
        var user = new FakeUserContext { TextCasePreference = preference };
        var handler = new CreateWhatsAppContactHandler(
            repo, new FakeAuditLogService(), user, new UserTextNormalizationService(user));
        return (handler, repo, user);
    }

    [Fact]
    public async Task TelefonNormalizeEdilerekKaydedilir()
    {
        var (handler, repo, _) = CreateHandler();
        var result = await handler.HandleAsync(new CreateWhatsAppContactRequest
        {
            FullName = "Murat Test",
            Phone    = "0532 123 45 67"
        });

        Assert.True(result.Success);
        Assert.Equal("+905321234567", repo.Items.Single().Phone);
    }

    [Fact]
    public async Task FarkliFormattaAyniNumara_MukerrerSayilirVeAnlasilirHataVerir()
    {
        var (handler, _, _) = CreateHandler();
        await handler.HandleAsync(new CreateWhatsAppContactRequest
        {
            FullName = "Murat Test", Phone = "0532 123 45 67"
        });

        var duplicate = await handler.HandleAsync(new CreateWhatsAppContactRequest
        {
            FullName = "Başka Kayıt", Phone = "+90 532 123 45 67"
        });

        Assert.False(duplicate.Success);
        Assert.Contains("zaten kayıtlıdır", duplicate.ErrorMessage);
        Assert.Contains("Murat Test", duplicate.ErrorMessage); // mevcut kişi kullanıcıya gösterilir
    }

    [Fact]
    public async Task GecersizTelefon_Reddedilir()
    {
        var (handler, _, _) = CreateHandler();
        var result = await handler.HandleAsync(new CreateWhatsAppContactRequest
        {
            FullName = "Test", Phone = "12345"
        });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SoftDeleteEdilmisNumara_YenidenEklenince_KayitGeriYuklenir()
    {
        var (handler, repo, _) = CreateHandler();
        await handler.HandleAsync(new CreateWhatsAppContactRequest
        {
            FullName = "Eski Kayıt", Phone = "5321234567"
        });

        var entity = repo.Items.Single();
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;

        var restored = await handler.HandleAsync(new CreateWhatsAppContactRequest
        {
            FullName = "Yeni Ad", Phone = "0532 123 45 67", Company = "Firma X"
        });

        Assert.True(restored.Success);
        Assert.Single(repo.Items); // yeni kayıt açılmaz, mevcut geri yüklenir
        Assert.False(entity.IsDeleted);
        Assert.True(entity.IsActive);
        Assert.Equal("Yeni Ad", entity.FullName);
    }

    [Fact]
    public async Task AdVeFirma_HarfTercihineTabidir_TelefonDegismez()
    {
        var (handler, repo, _) = CreateHandler(TextCasePreference.Uppercase);
        await handler.HandleAsync(new CreateWhatsAppContactRequest
        {
            FullName = "murat ışık",
            Phone    = "0532 123 45 67",
            Company  = "izmir lojistik"
        });

        var entity = repo.Items.Single();
        Assert.Equal("MURAT IŞIK", entity.FullName);
        Assert.Equal("İZMİR LOJİSTİK", entity.Company);
        Assert.Equal("+905321234567", entity.Phone); // telefon harf dönüşümünden muaf
    }

    // ── Arama (m / mu / mur → ad, firma veya telefonda eşleşme) ─────────────

    private static readonly List<WhatsAppContactDto> SearchSet =
    [
        new() { FullName = "Murat Capan",  Company = "Erdemsoft",  Phone = "+905321111111" },
        new() { FullName = "Ali Veli",     Company = "Murmar A.Ş.", Phone = "+905322222222" },
        new() { FullName = "Ayşe Yılmaz",  Company = "Beta",        Phone = "+905332223344" },
    ];

    [Theory]
    [InlineData("m")]
    [InlineData("mu")]
    [InlineData("mur")]
    public void Arama_KismiGiriste_AdVeyaFirmadanEslesir(string term)
    {
        var result = WhatsAppContactSearch.Filter(SearchSet, term);

        Assert.Contains(result, c => c.FullName == "Murat Capan");   // ad eşleşmesi
        Assert.Contains(result, c => c.Company  == "Murmar A.Ş.");   // firma eşleşmesi
    }

    [Fact]
    public void Arama_TelefonlaCalisir()
    {
        var result = WhatsAppContactSearch.Filter(SearchSet, "533222");
        Assert.Single(result);
        Assert.Equal("Ayşe Yılmaz", result[0].FullName);
    }

    [Fact]
    public void Arama_BosTerim_TumListeyiDoner()
    {
        Assert.Equal(SearchSet.Count, WhatsAppContactSearch.Filter(SearchSet, "  ").Count);
    }

    [Fact]
    public async Task PasifVeSilinmisKayitlar_NormalSecimListesindeGorunmez()
    {
        var (handler, repo, _) = CreateHandler();
        await handler.HandleAsync(new CreateWhatsAppContactRequest { FullName = "Aktif",  Phone = "5321111111" });
        await handler.HandleAsync(new CreateWhatsAppContactRequest { FullName = "Pasif",  Phone = "5322222222" });
        await handler.HandleAsync(new CreateWhatsAppContactRequest { FullName = "Silinmiş", Phone = "5323333333" });

        repo.Items.Single(x => x.FullName == "Pasif").IsActive     = false;
        repo.Items.Single(x => x.FullName == "Silinmiş").IsDeleted = true;

        var listHandler = new Application.Features.WhatsAppContacts.GetWhatsAppContactList.GetWhatsAppContactListHandler(repo);
        var visible = await listHandler.HandleAsync(new Application.Features.WhatsAppContacts.GetWhatsAppContactList.GetWhatsAppContactListQuery());

        Assert.Single(visible);
        Assert.Equal("Aktif", visible[0].FullName);
    }
}
