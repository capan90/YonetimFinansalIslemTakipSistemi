using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.CreateWhatsAppContact;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.DeleteWhatsAppContact;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.UpdateWhatsAppContact;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Ortak WhatsApp rehberi yazma yetkisi — Sprint 17 güvenlik guard'ı.
/// Kargo yönetimi veya firma rehberi yönetimi izinlerinden biri gerekir.
/// </summary>
public class WhatsAppContactPermissionTests
{
    private static FakeUserContext UserWith(params PermissionType[] permissions)
    {
        var user = new FakeUserContext();
        user.SetUser(user.UserId, user.FullName, new HashSet<PermissionType>(permissions));
        return user;
    }

    private static WhatsAppContact SeedContact(FakeWhatsAppContactRepository repo)
    {
        var contact = new WhatsAppContact
        {
            Id       = Guid.NewGuid(),
            FullName = "Mevcut Kişi",
            Phone    = "905321234567",
            IsActive = true
        };
        repo.Items.Add(contact);
        return contact;
    }

    [Fact]
    public async Task Create_YetkisizKullanici_Reddedilir()
    {
        var repo    = new FakeWhatsAppContactRepository();
        var handler = new CreateWhatsAppContactHandler(
            repo, new FakeAuditLogService(), UserWith(), new FakeTextNormalizationService());

        var result = await handler.HandleAsync(new CreateWhatsAppContactRequest
        {
            FullName = "Yeni Kişi", Phone = "0532 123 45 67"
        });

        Assert.False(result.Success);
        Assert.Empty(repo.Items);
    }

    [Theory]
    [InlineData(PermissionType.CanManageIncomingCargo)]
    [InlineData(PermissionType.CanManageOutgoingCargo)]
    [InlineData(PermissionType.CanManageCompanyDirectory)]
    public async Task Create_KabulEdilenIzinlerdenBiriVarsa_Basarili(PermissionType permission)
    {
        var repo    = new FakeWhatsAppContactRepository();
        var handler = new CreateWhatsAppContactHandler(
            repo, new FakeAuditLogService(), UserWith(permission), new FakeTextNormalizationService());

        var result = await handler.HandleAsync(new CreateWhatsAppContactRequest
        {
            FullName = "Yeni Kişi", Phone = "0532 123 45 67"
        });

        Assert.True(result.Success);
        Assert.Single(repo.Items);
    }

    [Fact]
    public async Task Update_YetkisizKullanici_Reddedilir()
    {
        var repo    = new FakeWhatsAppContactRepository();
        var contact = SeedContact(repo);
        var handler = new UpdateWhatsAppContactHandler(
            repo, new FakeAuditLogService(), UserWith(), new FakeTextNormalizationService());

        var result = await handler.HandleAsync(new UpdateWhatsAppContactRequest
        {
            Id = contact.Id, FullName = "Değişen Ad", Phone = "0532 123 45 67", IsActive = true
        });

        Assert.False(result.Success);
        Assert.Equal("Mevcut Kişi", contact.FullName);
    }

    [Fact]
    public async Task Delete_YetkisizKullanici_Reddedilir_KayitSilinmez()
    {
        var repo    = new FakeWhatsAppContactRepository();
        var contact = SeedContact(repo);
        var handler = new DeleteWhatsAppContactHandler(repo, new FakeAuditLogService(), UserWith());

        var result = await handler.HandleAsync(contact.Id);

        Assert.False(result.Success);
        Assert.False(contact.IsDeleted);
    }

    [Fact]
    public async Task Delete_YetkiliKullanici_SoftDeleteUygulanir()
    {
        var repo    = new FakeWhatsAppContactRepository();
        var contact = SeedContact(repo);
        var audit   = new FakeAuditLogService();
        var handler = new DeleteWhatsAppContactHandler(
            repo, audit, UserWith(PermissionType.CanManageCompanyDirectory));

        var result = await handler.HandleAsync(contact.Id);

        Assert.True(result.Success);
        Assert.True(contact.IsDeleted);
        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AuditAction.WhatsAppContactDeleted, entry.Action);
    }
}
