using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.CreateCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Commands.CreateCompanyDirectory;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Tests;

/// <summary>
/// Hangi alanların harf dönüşümüne tabi olduğu handler seviyesinde açıkça belirlenir:
/// anlamlı iş metinleri dönüştürülür; e-posta, telefon, URL ve kod alanları dönüştürülmez.
/// </summary>
public class NormalizationFieldScopeTests
{
    private sealed class FakeCompanyDirectoryRepository : ICompanyDirectoryRepository
    {
        public List<CompanyDirectory> Items { get; } = [];

        public Task<CompanyDirectory?> GetByIdAsync(Guid id)
            => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
        public Task<CompanyDirectory?> GetByIdWithTrackingAsync(Guid id) => GetByIdAsync(id);
        public Task<IReadOnlyList<CompanyDirectory>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<CompanyDirectory>>(Items.ToList());
        public Task AddAsync(CompanyDirectory entity) { Items.Add(entity); return Task.CompletedTask; }
        public Task UpdateAsync(CompanyDirectory entity) => Task.CompletedTask;
    }

    [Fact]
    public async Task FirmaRehberi_MetinAlanlariDonusur_EpostaTelefonDonusmez()
    {
        var repo = new FakeCompanyDirectoryRepository();
        var user = new FakeUserContext { TextCasePreference = TextCasePreference.Uppercase };
        user.GrantAll();
        var handler = new CreateCompanyDirectoryHandler(
            repo, new FakeAuditLogService(), user, new UserTextNormalizationService(user));

        var result = await handler.HandleAsync(new CreateCompanyDirectoryRequest
        {
            CompanyName = "ışık ticaret",
            AddressLine = "atatürk cad. no 5",
            City        = "istanbul",
            District    = "kadıköy",
            AttentionTo = "murat bey",
            Phone       = "0212 555 44 33",
            Email       = "Info@IsikTicaret.com",
            PostalCode  = "34710"
        });

        Assert.True(result.Success);
        var entity = repo.Items.Single();

        // Dönüştürülen alanlar (tr-TR BÜYÜK HARF)
        Assert.Equal("IŞIK TİCARET", entity.CompanyName);
        Assert.Equal("ATATÜRK CAD. NO 5", entity.AddressLine);
        Assert.Equal("İSTANBUL", entity.City);
        Assert.Equal("KADIKÖY", entity.District);
        Assert.Equal("MURAT BEY", entity.AttentionTo);

        // Muaf alanlar: telefon aynen, e-posta mevcut standardıyla (lowercase invariant)
        Assert.Equal("0212 555 44 33", entity.Phone);
        Assert.Equal("info@isikticaret.com", entity.Email);
        Assert.Equal("34710", entity.PostalCode);
    }

    [Fact]
    public async Task KargoKaydi_PlakaDonusur_TakipNoVeUrlDonusmez()
    {
        var repo = new FakeCargoShipmentRepository();
        var user = new FakeUserContext { TextCasePreference = TextCasePreference.Uppercase };
        user.GrantAll();
        var handler = new CreateCargoShipmentHandler(
            repo, new FakeCargoCompanyRepository(), new FakeAuditLogService(),
            user, new FakeCargoDashboardCache(), new UserTextNormalizationService(user));

        var result = await handler.HandleAsync(new CreateCargoShipmentRequest
        {
            Direction      = CargoShipmentDirection.Outgoing,
            ShipmentDate   = DateTime.Today,
            Status         = CargoShipmentStatus.Prepared,
            VehiclePlate   = "34 abc 123",
            SenderName     = "ışıl hanım",
            TrackingNumber = "TrK-0042x",
            TrackingUrl    = "https://Kargo.com/Takip/TrK-0042x"
        });

        Assert.True(result.Success);
        var entity = repo.Added.Single();

        Assert.Equal("34 ABC 123", entity.VehiclePlate);   // plaka dönüştürülür
        Assert.Equal("IŞIL HANIM", entity.SenderName);
        Assert.Equal("TrK-0042x", entity.TrackingNumber);  // kod alanı dönüştürülmez
        Assert.Equal("https://Kargo.com/Takip/TrK-0042x", entity.TrackingUrl); // URL dönüştürülmez
    }
}
