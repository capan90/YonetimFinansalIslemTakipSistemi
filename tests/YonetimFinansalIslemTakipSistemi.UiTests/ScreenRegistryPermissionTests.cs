using System.Text;
using System.Text.RegularExpressions;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Kabuk yetki eşlemesinin satır satır doğrulaması.
///
/// NEDEN VAR: Kabuk navigasyonu, ekranların BUGÜNKÜ görünürlük kapılarını
/// birebir sürdürmek zorunda. Aksi hâlde tek kabuğa geçiş sessizce bir yetki
/// değişikliği yapar — kimse fark etmeden bir kullanıcı görmemesi gereken
/// ekranı görür ya da gördüğünü kaybeder.
///
/// Aşağıdaki tablo mevcut koddan çıkarıldı:
///   MainWindow.RefreshMenuVisibility()
///   CargoDashboardWindow.ApplyNavBarVisibility()
///   App.ResolveStartupMode()
///
/// İlk taslakta iki satır VARSAYIMDI ve yanlıştı:
///   Sistem Sağlığı  → CanManageUsers varsayılmıştı; gerçekte
///                     CanViewSystemLogs VEYA CanAccessSettings
///   WhatsApp/Mail   → CanViewCargoModule varsayılmıştı; gerçekte ayrı kapı
///                     yok, kargo kullanıcısının tamamına açık
/// </summary>
public class ScreenRegistryPermissionTests
{
    /// <summary>
    /// Beklenen eşleme. Kaynak koddaki kapı değişirse BU TABLO da
    /// değişmelidir — testin amacı sapmayı görünür kılmak.
    /// </summary>
    public static TheoryData<ScreenKey, PermissionType[]> ExpectedPermissions() => new()
    {
        // Nakit İşlemler: bugün MainWindow'u kim görüyorsa o görüyor.
        // Küme App.ResolveStartupMode'daki "finance" tanımının aynısı.
        {
            ScreenKey.CashTransactions,
            [
                PermissionType.CanCreateTransaction,
                PermissionType.CanEditTransaction,
                PermissionType.CanDeleteTransaction,
                PermissionType.CanViewReports,
                PermissionType.CanManageUsers,
                PermissionType.CanViewAuditLog,
                PermissionType.CanManageExchangeRates,
            ]
        },

        // MenuItemAnaliz / MenuItemRaporlar → canReports
        { ScreenKey.Analysis,      [PermissionType.CanViewReports] },
        { ScreenKey.Reports,       [PermissionType.CanViewReports] },

        // MenuItemDoviz → canExchange
        { ScreenKey.ExchangeRates, [PermissionType.CanManageExchangeRates] },

        // MenuItemKargoDashboard → CanViewCargoModule (tek başına)
        { ScreenKey.CargoDashboard, [PermissionType.CanViewCargoModule] },

        // MenuItemGelenKargolar / NavGelenButton
        {
            ScreenKey.IncomingCargo,
            [PermissionType.CanViewIncomingCargo, PermissionType.CanManageIncomingCargo]
        },

        // MenuItemGidenKargolar / NavGidenButton
        {
            ScreenKey.OutgoingCargo,
            [PermissionType.CanViewOutgoingCargo, PermissionType.CanManageOutgoingCargo]
        },

        // PARAMETRELİ ekran. Operasyon, kargo listesinden erişilen bir eylem;
        // yetki kapısı o listelerin kapılarının birleşimi.
        {
            ScreenKey.CargoOperationCenter,
            [
                PermissionType.CanViewIncomingCargo, PermissionType.CanManageIncomingCargo,
                PermissionType.CanViewOutgoingCargo, PermissionType.CanManageOutgoingCargo,
            ]
        },

        // MenuItemFirmaRehberi / NavFirmaRehberiButton
        {
            ScreenKey.CompanyDirectory,
            [PermissionType.CanManageCompanyDirectory, PermissionType.CanViewCargoModule]
        },

        // MenuItemKargoFirmalari / NavKargoFirmalariButton
        {
            ScreenKey.CargoCompanies,
            [PermissionType.CanManageCargoCompanies, PermissionType.CanViewCargoModule]
        },

        // WhatsApp ve Mail rehberi: ayrı kapı YOK. Kargo menüsü/şeridi
        // görünüyorsa görünüyorlar → kargo kullanıcısı kümesinin tamamı.
        { ScreenKey.WhatsAppContacts, CargoUserSet },
        { ScreenKey.MailContacts,     CargoUserSet },

        // MenuItemKullanicilar / MenuItemYetkiler → canManage
        { ScreenKey.Users,       [PermissionType.CanManageUsers] },
        { ScreenKey.Permissions, [PermissionType.CanManageUsers] },

        // MenuItemDenetim → canAudit
        { ScreenKey.AuditLog, [PermissionType.CanViewAuditLog] },

        // MenuItemSistemLoglari → canSystemLogs
        { ScreenKey.SystemLogs, [PermissionType.CanViewSystemLogs] },

        // MenuItemSistemSagligi → canSystemHealth = canSystemLogs || canAccessSettings
        {
            ScreenKey.SystemHealth,
            [PermissionType.CanViewSystemLogs, PermissionType.CanAccessSettings]
        },
    };

    private static readonly PermissionType[] CargoUserSet =
    [
        PermissionType.CanViewCargoModule,
        PermissionType.CanViewIncomingCargo,
        PermissionType.CanManageIncomingCargo,
        PermissionType.CanViewOutgoingCargo,
        PermissionType.CanManageOutgoingCargo,
        PermissionType.CanManageCompanyDirectory,
        PermissionType.CanManageCargoCompanies,
    ];

    [Theory]
    [MemberData(nameof(ExpectedPermissions))]
    public void Ekranin_yetki_kumesi_beklenenle_ayni(ScreenKey key, PermissionType[] expected)
    {
        var screen = ScreenRegistry.All.SingleOrDefault(s => s.Key == key);
        Assert.True(screen is not null, $"{key} kayıt tablosunda yok.");

        Assert.Equal(
            expected.OrderBy(p => p).ToArray(),
            screen!.RequiredPermissions.OrderBy(p => p).ToArray());
    }

    /// <summary>
    /// Tabloda olmayan ekran kalmasın — yeni ekran eklenip yetkisi
    /// doğrulanmadan geçmesin.
    /// </summary>
    [Fact]
    public void Her_kayitli_ekranin_beklenen_yetkisi_tanimli()
    {
        var covered = ExpectedPermissions().Select(row => (ScreenKey)row[0]!).ToHashSet();
        var missing = ScreenRegistry.All.Select(s => s.Key).Where(k => !covered.Contains(k)).ToList();

        Assert.True(missing.Count == 0,
            "Yetki eşlemesi doğrulanmamış ekran(lar): " + string.Join(", ", missing));
    }

    /// <summary>
    /// Hiçbir ekran yetkisiz bırakılmamalı. Boş liste "herkese açık" demektir
    /// ve şu an hiçbir sekme ekranı için doğru değil — kişisel ayarlar modal
    /// olarak kalıyor.
    /// </summary>
    [Fact]
    public void Hicbir_sekme_ekrani_yetkisiz_degil()
    {
        var open = ScreenRegistry.All.Where(s => s.RequiredPermissions.Count == 0).ToList();

        Assert.True(open.Count == 0,
            "Yetki aramayan sekme ekranı: " + string.Join(", ", open.Select(s => s.Key)));
    }

    /// <summary>
    /// Kabuk mevcut permission modelinde OLMAYAN bir yetki kullanmamalı.
    /// Enum'ı okuyup karşılaştırır; kabuk kendine yetki uyduramaz.
    /// </summary>
    [Fact]
    public void Kullanilan_yetkiler_permission_modelinde_tanimli()
    {
        var enumPath = Path.Combine(
            UiSourceLocator.UiProjectDirectory, "..",
            "YonetimFinansalIslemTakipSistemi.Domain", "Enums", "PermissionType.cs");

        var declared = Regex.Matches(File.ReadAllText(enumPath, Encoding.UTF8),
                                     @"^\s*(?<name>Can\w+)\s*=", RegexOptions.Multiline)
                            .Select(m => m.Groups["name"].Value)
                            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(declared);

        var used = ScreenRegistry.All
            .SelectMany(s => s.RequiredPermissions)
            .Select(p => p.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var unknown = used.Where(u => !declared.Contains(u)).ToList();
        Assert.True(unknown.Count == 0,
            "Permission modelinde olmayan yetki: " + string.Join(", ", unknown));
    }

    /// <summary>
    /// Kargo Operasyon Merkezi PARAMETRELİ olarak işaretli olmalı: bağımsız bir
    /// ekran değil, seçili bir kargo kaydı üzerinde çalışıyor
    /// (<c>CargoOperationCenterWindow(IServiceProvider, CargoShipmentDto)</c>).
    ///
    /// Tekil olarak işaretlenirse navigasyon rayında görünür ve tıklandığında
    /// hangi kaydı açacağı belirsiz kalırdı.
    /// </summary>
    [Fact]
    public void Operasyon_merkezi_parametreli_ekran_olarak_isaretli()
    {
        var screen = ScreenRegistry.All.Single(s => s.Key == ScreenKey.CargoOperationCenter);

        Assert.True(screen.IsParameterized,
            "Operasyon Merkezi kayıt üzerinde çalışır; tekil ekran olarak işaretlenemez.");
    }

    /// <summary>
    /// Parametreli olmayan her ekran tekil açılabilmeli — ikisi birden
    /// tanımlanamaz, hiçbiri tanımlanmazsa ekran taşınmamış sayılır.
    /// </summary>
    [Fact]
    public void Bir_ekran_hem_tekil_hem_parametreli_olamaz()
    {
        var both = ScreenRegistry.All
            .Where(s => s.CreateView is not null && s.CreateInstance is not null)
            .ToList();

        Assert.True(both.Count == 0,
            "Hem CreateView hem CreateInstance tanımlı: " + string.Join(", ", both.Select(s => s.Key)));
    }
}
