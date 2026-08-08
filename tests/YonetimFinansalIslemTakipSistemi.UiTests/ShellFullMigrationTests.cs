using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Shell;
using static YonetimFinansalIslemTakipSistemi.UiTests.ShellTestDoubles;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Faz D6: bütün ekranlar kabuğa taşındı, iki eski kabuk da başlangıç
/// akışından çıktı.
///
/// Bu testlerin derdi TAM GEÇİŞİN sessiz kayıplarıdır:
///   • bir ekranın kayıt tablosuna girmemesi → kullanıcı ona hiç ulaşamaz
///   • menüdeki bir eylemin kabuğa taşınmaması → aynı sonuç
///   • aynı işin hem ekranda hem kabukta durması → biri düzeltilince diğeri geride kalır
///
/// Kabuk kurulumu gerçek <see cref="ShellWindow"/> üzerinden sınanır; sekme
/// içerikleri sahtedir çünkü gerçek ekranlar tüm veri katmanını ister.
/// </summary>
public class ShellFullMigrationTests
{
    // ── Yardımcılar ──────────────────────────────────────────────────────
    //
    // Sahte nesneler ShellTestDoubles'ta (Faz E9).

    /// <summary>Gerçek kayıt tablosunun sahte görünümlü kopyası.</summary>
    private static IReadOnlyList<ScreenDefinition> StubRegistry() =>
        ScreenRegistry.All.Select(s => s with
        {
            CreateView     = s.IsParameterized ? null : _ => new UserControl(),
            CreateInstance = s.IsParameterized
                ? (_, p) => new ScreenInstance(p.ToString()!, s.Title, new UserControl())
                : null,
        }).ToList();

    private static ShellViewModel Vm(params PermissionType[] permissions)
        => Shell(StubRegistry(), permissions);

    /// <summary>Kargo-only kullanıcının yetki kümesi — App.ResolveStartupMode "cargo" der.</summary>
    private static readonly PermissionType[] CargoOnly =
    [
        PermissionType.CanViewCargoModule,
        PermissionType.CanViewIncomingCargo,
        PermissionType.CanManageOutgoingCargo,
    ];

    // ── Kayıt tablosu tam mı ─────────────────────────────────────────────

    /// <summary>
    /// Her <see cref="ScreenKey"/> kayıt tablosunda olmalı. Enum'a eklenip
    /// tabloya girmeyen bir ekran hiçbir yerden açılamaz.
    /// </summary>
    [Fact]
    public void Her_ScreenKey_kayit_tablosunda()
    {
        var registered = ScreenRegistry.All.Select(s => s.Key).ToHashSet();
        var missing    = Enum.GetValues<ScreenKey>().Where(k => !registered.Contains(k)).ToList();

        Assert.True(missing.Count == 0,
            "Kayıt tablosunda olmayan ekran(lar): " + string.Join(", ", missing));
    }

    /// <summary>
    /// Rayda görünen her ekranın bir grubu olmalı; grupsuz öğe menü
    /// karşılığını kaybeder ve rayda başlıksız kalır.
    /// </summary>
    [Fact]
    public void Her_ekranin_navigasyon_grubu_var()
    {
        var ungrouped = ScreenRegistry.All
            .Where(s => string.IsNullOrWhiteSpace(s.NavGroup))
            .Select(s => s.Key)
            .ToList();

        Assert.True(ungrouped.Count == 0,
            "Navigasyon grubu tanımsız ekran(lar): " + string.Join(", ", ungrouped));
    }

    // ── Navigasyon rayı ──────────────────────────────────────────────────

    [Fact]
    public void Bos_grup_rayda_gorunmez() => ThemeTestHost.Run(() =>
    {
        var vm = Vm(CargoOnly);

        Assert.All(vm.NavigationGroups, g => Assert.NotEmpty(g.Screens));

        // Kargo kullanıcısında finans ve yönetim grupları hiç oluşmamalı
        var groups = vm.NavigationGroups.Select(g => g.Title).ToList();
        Assert.DoesNotContain("Finans", groups);
        Assert.DoesNotContain("Yönetim", groups);
        Assert.Contains("Kargo Takip", groups);
    });

    /// <summary>
    /// Grup sırası kayıt tablosundaki ilk görünme sırasıdır — alfabetik değil.
    /// Kullanıcının menüde alıştığı sıra korunmalı.
    /// </summary>
    [Fact]
    public void Grup_sirasi_kayit_tablosundan_gelir() => ThemeTestHost.Run(() =>
    {
        var vm = Vm(
            PermissionType.CanCreateTransaction,   // Finans
            PermissionType.CanViewCargoModule,     // Kargo Takip
            PermissionType.CanManageUsers,         // Yönetim
            PermissionType.CanViewSystemLogs);     // Ayarlar

        Assert.Equal(
            ["Finans", "Kargo Takip", "Yönetim", "Ayarlar"],
            vm.NavigationGroups.Select(g => g.Title).ToArray());
    });

    /// <summary>
    /// Parametreli ekran rayda görünmez: raydan tıklanınca hangi kaydı
    /// açacağı belli değil.
    /// </summary>
    [Fact]
    public void Operasyon_merkezi_rayda_gorunmez() => ThemeTestHost.Run(() =>
    {
        var vm = Vm(CargoOnly);

        Assert.DoesNotContain(vm.NavigationItems, s => s.Key == ScreenKey.CargoOperationCenter);
    });

    // ── Kargo kullanıcısı ────────────────────────────────────────────────

    /// <summary>
    /// Kargo-only kullanıcı finans ekranlarını ne rayda görür ne de
    /// programatik olarak açabilir.
    /// </summary>
    [Fact]
    public void Kargo_kullanicisi_finans_ekranlarina_ulasamaz() => ThemeTestHost.Run(() =>
    {
        var vm = Vm(CargoOnly);

        foreach (var key in new[] { ScreenKey.CashTransactions, ScreenKey.Analysis,
                                    ScreenKey.Reports, ScreenKey.ExchangeRates,
                                    ScreenKey.Users, ScreenKey.Permissions })
        {
            Assert.DoesNotContain(vm.NavigationItems, s => s.Key == key);
            Assert.Null(vm.OpenScreen(key));
        }

        Assert.Empty(vm.Tabs);
    });

    /// <summary>
    /// Kargo-only kullanıcının kargo ekranlarına erişimi KAYBOLMAMALI —
    /// eski Kargo Dashboard şeridindeki her hedef rayda olmalı.
    /// </summary>
    [Theory]
    [InlineData(ScreenKey.CargoDashboard)]
    [InlineData(ScreenKey.IncomingCargo)]
    [InlineData(ScreenKey.CompanyDirectory)]
    [InlineData(ScreenKey.CargoCompanies)]
    [InlineData(ScreenKey.WhatsAppContacts)]
    public void Kargo_kullanicisinin_ekranlari_rayda(ScreenKey key) => ThemeTestHost.Run(() =>
    {
        var vm = Vm(CargoOnly);

        Assert.Contains(vm.NavigationItems, s => s.Key == key);
    });

    // ── Parametreli sekme ────────────────────────────────────────────────

    /// <summary>
    /// Farklı kargolar ayrı sekmelerde; AYNI kargo ikinci kez açılınca yeni
    /// sekme oluşmaz.
    /// </summary>
    [Fact]
    public void Ayni_kayit_ikinci_sekme_uretmez() => ThemeTestHost.Run(() =>
    {
        var vm = Vm(CargoOnly);

        var first  = vm.OpenScreen(ScreenKey.CargoOperationCenter, "KRG-1");
        var second = vm.OpenScreen(ScreenKey.CargoOperationCenter, "KRG-2");
        var again  = vm.OpenScreen(ScreenKey.CargoOperationCenter, "KRG-1");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(2, vm.Tabs.Count);
        Assert.Same(first, again);
        Assert.Same(first, vm.ActiveTab);
    });

    // ── Ekranın kapanma isteği ───────────────────────────────────────────

    private sealed class ClosableScreen : UserControl, IShellCloseSource
    {
        public event Action? CloseRequested;
        public void RaiseClose() => CloseRequested?.Invoke();
    }

    /// <summary>
    /// Ekrandaki "Kapat" düğmesi kabukta SEKMEYİ kapatır — pencereyi değil.
    /// </summary>
    [Fact]
    public void Ekran_kapatma_istegi_sekmeyi_kapatir() => ThemeTestHost.Run(() =>
    {
        var screen = new ClosableScreen();

        var screens = new[]
        {
            new ScreenDefinition(ScreenKey.IncomingCargo, "Gelen", [PermissionType.CanViewIncomingCargo],
                                 CreateView: _ => screen),
        };

        var vm = new ShellViewModel(
            new FakeServices(new FakeUserContext(PermissionType.CanViewIncomingCargo)),
            new FakeUserContext(PermissionType.CanViewIncomingCargo),
            screens);

        vm.OpenScreen(ScreenKey.IncomingCargo);
        Assert.Single(vm.Tabs);

        screen.RaiseClose();

        Assert.Empty(vm.Tabs);
    });

    // ── Kaynak sözleşmeleri: kopya kalmadı mı ────────────────────────────

    private static IEnumerable<string> FilesContaining(string needle) =>
        UiSourceLocator.CsFiles()
            .Where(p => File.ReadAllText(p, Encoding.UTF8).Contains(needle, StringComparison.Ordinal))
            .Select(UiSourceLocator.Relative)
            .OrderBy(p => p, StringComparer.Ordinal);

    /// <summary>
    /// Manuel güncelleme kontrolü tek yerde. MainWindow ve Kargo Panosu'nda
    /// BİREBİR aynı 50 satır iki kez duruyordu; kabuk üçüncü giriş noktası
    /// olunca ortaklaştırıldı.
    /// </summary>
    [Fact]
    public void Guncelleme_kontrolu_tek_yerde()
    {
        var owners = FilesContaining("IsClickOnceDeployment").ToList();

        Assert.Contains("Common/UpdateCheckFlow.cs", owners);
        Assert.DoesNotContain("MainWindow.xaml.cs", owners);
        Assert.DoesNotContain("Views/Cargo/CargoDashboardScreen.xaml.cs", owners);
        Assert.DoesNotContain("Views/Shell/ShellWindow.xaml.cs", owners);
    }

    /// <summary>Log klasörünü açma akışı tek yerde.</summary>
    [Fact]
    public void Log_klasoru_akisi_tek_yerde()
    {
        var owners = FilesContaining("Log klasörü oluşturulamadı").ToList();

        Assert.Equal(["Common/ToolActions.cs"], owners);
    }

    /// <summary>
    /// UserLoggedOut denetim kaydı tek yerde — üç kabuk penceresi de aynı
    /// yardımcıyı kullanmalı.
    /// </summary>
    [Fact]
    public void Cikis_audit_kaydi_tek_yerde()
    {
        // Kaydı YAZAN yerler aranıyor. AuditLogViewModel enum'ı yalnızca
        // filtre açılırında listeliyor — o bir yazıcı değil.
        var writers = UiSourceLocator.CsFiles()
            .Where(p =>
            {
                var text = File.ReadAllText(p, Encoding.UTF8);
                return text.Contains("AuditAction.UserLoggedOut", StringComparison.Ordinal)
                    && text.Contains("WriteAsync", StringComparison.Ordinal);
            })
            .Select(UiSourceLocator.Relative)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(["Common/SessionLogout.cs"], writers);
    }

    /// <summary>
    /// Açılıştaki otomatik güncelleme kontrolü kabuğun işi. Ekranlarda
    /// koşulsuz çalışırsa kullanıcı aynı bildirimi iki kez görür.
    /// </summary>
    [Fact]
    public void Acilis_guncelleme_kontrolu_kabukta()
    {
        var shell = File.ReadAllText(
            Path.Combine(UiSourceLocator.UiProjectDirectory, "Views", "Shell", "ShellWindow.xaml.cs"), Encoding.UTF8);

        Assert.Contains("StartupUpdateChecker.RunOnceAsync", shell, StringComparison.Ordinal);

        // Açılış kontrolü TEK YERDE. Kargo panosu ekranı barındırıcı pencere
        // döneminde kendi kontrolünü yapıyordu; o pencere kaldırıldı (Faz F1)
        // ve ekranda hiç çağrı kalmadı — kalsaydı kullanıcı aynı bildirimi
        // iki kez görürdü.
        var dashboard = File.ReadAllText(
            Path.Combine(UiSourceLocator.UiProjectDirectory, "Views", "Cargo", "CargoDashboardScreen.xaml.cs"),
            Encoding.UTF8);

        Assert.DoesNotContain("StartupUpdateChecker.RunOnceAsync", dashboard, StringComparison.Ordinal);
    }

    // ── İnce barındırıcılar ──────────────────────────────────────────────

    /// <summary>
    /// Ekranı barındıran ESKİ PENCERELER SİLİNDİ. Faz D'de ince barındırıcıya
    /// dönüştürülüp geri dönüş yolu olarak bırakılmışlardı; Faz E'de
    /// donduruldular, Faz F1'de kaldırıldılar.
    ///
    /// Bu test dosyaların YOKLUĞUNU sabitler: biri geri gelirse uygulamada
    /// ikinci bir gezinme modeli doğar ve hangi yolun canlı olduğu belirsizleşir.
    /// </summary>
    [Theory]
    [InlineData("MainWindow.xaml")]
    [InlineData("MainWindow.xaml.cs")]
    [InlineData("Views/Analysis/AnalysisWindow.xaml.cs")]
    [InlineData("Views/Reports/ReportWindow.xaml.cs")]
    [InlineData("Views/ExchangeRates/ExchangeRateWindow.xaml.cs")]
    [InlineData("Views/Cargo/CargoDashboardWindow.xaml.cs")]
    [InlineData("Views/Cargo/CargoShipmentListWindow.xaml.cs")]
    [InlineData("Views/Cargo/CargoOperationCenterWindow.xaml.cs")]
    [InlineData("Views/Cargo/CompanyDirectoryListWindow.xaml.cs")]
    [InlineData("Views/Cargo/CargoCompanyListWindow.xaml.cs")]
    [InlineData("Views/WhatsApp/WhatsAppContactListWindow.xaml.cs")]
    [InlineData("Views/Mail/MailContactListWindow.xaml.cs")]
    [InlineData("Views/Users/UserManagementWindow.xaml.cs")]
    [InlineData("Views/Permissions/UserPermissionWindow.xaml.cs")]
    [InlineData("Views/AuditLogs/AuditLogWindow.xaml.cs")]
    [InlineData("Views/SystemLogs/SystemLogsWindow.xaml.cs")]
    [InlineData("Views/Health/SystemHealthWindow.xaml.cs")]
    public void Legacy_pencere_geri_gelmedi(string relativePath)
    {
        var path = Path.Combine(UiSourceLocator.UiProjectDirectory,
                                relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.False(File.Exists(path),
            $"{relativePath} geri gelmiş — ekranlar yalnızca kabuk sekmesi olarak açılmalı.");
    }

    /// <summary>
    /// Ekranlar alt diyaloglarının sahibini AĞAÇTAN bulmalı; sabit pencereye
    /// bağlanan ekran kabukta sahipsiz diyalog açar.
    /// </summary>
    [Fact]
    public void Ekranlar_sahibi_agactan_buluyor()
    {
        var offenders = UiSourceLocator.CsFiles()
            .Where(p => UiSourceLocator.Relative(p).Contains("Screen.xaml.cs", StringComparison.Ordinal))
            .Where(p => Regex.IsMatch(File.ReadAllText(p, Encoding.UTF8), @"Owner\s*=\s*this\b"))
            .Select(UiSourceLocator.Relative)
            .ToList();

        Assert.True(offenders.Count == 0,
            "Owner = this kullanan ekran(lar): " + string.Join(", ", offenders));
    }
}
