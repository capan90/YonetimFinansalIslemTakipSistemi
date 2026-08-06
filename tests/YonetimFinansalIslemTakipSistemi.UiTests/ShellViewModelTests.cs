using System.Windows;
using System.Windows.Controls;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Shell;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Kabuk sekme sözleşmesi.
///
/// Ekran listesi <see cref="ShellViewModel"/>'e DIŞARIDAN verildiği için kabuk
/// mantığı gerçek ekranlara bağımlı olmadan sınanabiliyor — Faz D'nin bu
/// adımında henüz hiçbir ekran UserControl'e taşınmadı.
///
/// Test gövdeleri STA thread'inde çalışır: ekran görünümleri WPF kontrolü
/// üretiyor ve WPF girdi altyapısı STA ister.
/// </summary>
public class ShellViewModelTests
{
    // ── Test yardımcıları ────────────────────────────────────────────────

    private sealed class FakeUserContext(params PermissionType[] permissions) : IUserContext
    {
        public Guid   UserId   => Guid.Empty;
        public string FullName => "Test Kullanıcı";
        public TextCasePreference TextCasePreference => TextCasePreference.Preserve;
        public IReadOnlySet<PermissionType> Permissions { get; } = permissions.ToHashSet();
        public bool HasPermission(PermissionType permission) => Permissions.Contains(permission);
    }

    /// <summary>Kapanmayı reddeden ekran — kaydedilmemiş değişiklik benzetimi.</summary>
    private sealed class BlockingScreen : UserControl, IShellScreen
    {
        public bool AllowClose    { get; set; }
        public int  CloseAttempts { get; private set; }

        public bool RequestClose()
        {
            CloseAttempts++;
            return AllowClose;
        }
    }

    private static ScreenDefinition Screen(
        ScreenKey key,
        PermissionType? permission = null,
        bool migrated = true,
        bool canClose = true,
        Func<IServiceProvider, FrameworkElement>? factory = null)
        => new(key, key.ToString(),
               permission is null ? [] : [permission.Value],
               migrated ? factory ?? (_ => new UserControl()) : null,
               canClose);

    /// <summary>
    /// ShellViewModel yalnızca IUserContext ve ekran listesine bağlı;
    /// IServiceProvider ekran fabrikalarına geçilir ve testte kullanılmaz.
    /// </summary>
    private static ShellViewModel Build(
        IReadOnlyList<ScreenDefinition> screens,
        params PermissionType[] permissions)
        => new(services: null!, new FakeUserContext(permissions), screens);

    private static ShellViewModel ThreeTabs()
    {
        var vm = Build(
        [
            Screen(ScreenKey.CashTransactions, PermissionType.CanCreateTransaction),
            Screen(ScreenKey.Analysis,         PermissionType.CanViewReports),
            Screen(ScreenKey.AuditLog,         PermissionType.CanViewAuditLog),
        ],
        PermissionType.CanCreateTransaction, PermissionType.CanViewReports, PermissionType.CanViewAuditLog);

        vm.OpenScreen(ScreenKey.CashTransactions);
        vm.OpenScreen(ScreenKey.Analysis);
        vm.OpenScreen(ScreenKey.AuditLog);
        return vm;
    }

    // ── Navigasyon görünürlüğü (permission) ──────────────────────────────

    [Fact]
    public void Navigasyon_yalnizca_yetkili_ekranlari_gosterir() => ThemeTestHost.Run(() =>
    {
        var vm = Build(
        [
            Screen(ScreenKey.CashTransactions, PermissionType.CanCreateTransaction),
            Screen(ScreenKey.Users,            PermissionType.CanManageUsers),
            Screen(ScreenKey.AuditLog,         PermissionType.CanViewAuditLog),
        ],
        PermissionType.CanCreateTransaction, PermissionType.CanViewAuditLog);

        Assert.Equal(
            [ScreenKey.CashTransactions, ScreenKey.AuditLog],
            vm.NavigationItems.Select(n => n.Key));
    });

    [Fact]
    public void Yetki_gerektirmeyen_ekran_herkese_gorunur() => ThemeTestHost.Run(() =>
    {
        // Kişisel ayarlar gibi ekranlar yetki aramaz
        var vm = Build([Screen(ScreenKey.SystemHealth, permission: null)]);

        Assert.Single(vm.NavigationItems);
    });

    [Fact]
    public void Tasinmamis_ekran_navigasyonda_gorunmez() => ThemeTestHost.Run(() =>
    {
        // Gösterilseydi tıklandığında hiçbir şey olmazdı
        var vm = Build(
        [
            Screen(ScreenKey.CashTransactions, PermissionType.CanCreateTransaction),
            Screen(ScreenKey.Reports,          PermissionType.CanViewReports, migrated: false),
        ],
        PermissionType.CanCreateTransaction, PermissionType.CanViewReports);

        Assert.Single(vm.NavigationItems);
        Assert.Equal(ScreenKey.CashTransactions, vm.NavigationItems[0].Key);
    });

    // ── Yetkisiz ekran programatik olarak açılamaz ───────────────────────

    [Fact]
    public void Yetkisiz_ekran_programatik_olarak_acilamaz() => ThemeTestHost.Run(() =>
    {
        // Navigasyonda gizlemek YETMEZ: kısayol, komut paleti veya kod da
        // aynı kontrole takılmalı.
        var vm = Build([Screen(ScreenKey.Users, PermissionType.CanManageUsers)]);

        Assert.Null(vm.OpenScreen(ScreenKey.Users));
        Assert.Empty(vm.Tabs);
    });

    [Fact]
    public void Tasinmamis_ekran_programatik_olarak_acilamaz() => ThemeTestHost.Run(() =>
    {
        var vm = Build(
            [Screen(ScreenKey.Reports, PermissionType.CanViewReports, migrated: false)],
            PermissionType.CanViewReports);

        Assert.Null(vm.OpenScreen(ScreenKey.Reports));
        Assert.Empty(vm.Tabs);
    });

    [Fact]
    public void Kayitli_olmayan_ekran_acilamaz() => ThemeTestHost.Run(() =>
    {
        var vm = Build([Screen(ScreenKey.CashTransactions, PermissionType.CanCreateTransaction)],
                       PermissionType.CanCreateTransaction);

        Assert.Null(vm.OpenScreen(ScreenKey.SystemLogs));
    });

    // ── Sekme tekilliği ──────────────────────────────────────────────────

    [Fact]
    public void Ayni_ekran_ikinci_kez_acilirsa_yeni_sekme_olusmaz() => ThemeTestHost.Run(() =>
    {
        var vm = Build([Screen(ScreenKey.CashTransactions, PermissionType.CanCreateTransaction)],
                       PermissionType.CanCreateTransaction);

        var first  = vm.OpenScreen(ScreenKey.CashTransactions);
        var second = vm.OpenScreen(ScreenKey.CashTransactions);

        Assert.Single(vm.Tabs);
        Assert.Same(first, second);
    });

    [Fact]
    public void Ikinci_acilista_mevcut_sekmeye_odaklanilir() => ThemeTestHost.Run(() =>
    {
        var vm = Build(
        [
            Screen(ScreenKey.CashTransactions, PermissionType.CanCreateTransaction),
            Screen(ScreenKey.Analysis,         PermissionType.CanViewReports),
        ],
        PermissionType.CanCreateTransaction, PermissionType.CanViewReports);

        var cash = vm.OpenScreen(ScreenKey.CashTransactions);
        vm.OpenScreen(ScreenKey.Analysis);
        Assert.NotSame(cash, vm.ActiveTab);

        vm.OpenScreen(ScreenKey.CashTransactions);

        Assert.Same(cash, vm.ActiveTab);
        Assert.Equal(2, vm.Tabs.Count);
    });

    [Fact]
    public void Ikinci_acilista_gorunum_yeniden_uretilmez() => ThemeTestHost.Run(() =>
    {
        var created = 0;
        var vm = Build(
            [Screen(ScreenKey.CashTransactions, PermissionType.CanCreateTransaction,
                    factory: _ => { created++; return new UserControl(); })],
            PermissionType.CanCreateTransaction);

        vm.OpenScreen(ScreenKey.CashTransactions);
        vm.OpenScreen(ScreenKey.CashTransactions);

        Assert.Equal(1, created);
    });

    // ── Sekme kapatma ve aktif sekme ─────────────────────────────────────

    [Fact]
    public void Sekme_kapatilir_ve_aktif_sekme_komsuya_gecer() => ThemeTestHost.Run(() =>
    {
        var vm     = ThreeTabs();
        var second = vm.Tabs[1];
        vm.ActiveTab = second;

        Assert.True(vm.CloseTab(second));

        Assert.Equal(2, vm.Tabs.Count);
        Assert.DoesNotContain(second, vm.Tabs);
        Assert.NotNull(vm.ActiveTab);
    });

    [Fact]
    public void Son_sekme_kapaninca_aktif_sekme_bosalir() => ThemeTestHost.Run(() =>
    {
        var vm = Build([Screen(ScreenKey.Analysis, PermissionType.CanViewReports)],
                       PermissionType.CanViewReports);

        var tab = vm.OpenScreen(ScreenKey.Analysis)!;
        Assert.True(vm.CloseTab(tab));

        Assert.Empty(vm.Tabs);
        Assert.Null(vm.ActiveTab);
    });

    [Fact]
    public void CanClose_false_olan_sekme_kullanici_tarafindan_kapatilamaz() => ThemeTestHost.Run(() =>
    {
        var vm = Build(
            [Screen(ScreenKey.CashTransactions, PermissionType.CanCreateTransaction, canClose: false)],
            PermissionType.CanCreateTransaction);

        var tab = vm.OpenScreen(ScreenKey.CashTransactions)!;

        Assert.False(vm.CloseTab(tab));
        Assert.Single(vm.Tabs);
    });

    [Fact]
    public void Ekran_kapanmayi_reddedebilir() => ThemeTestHost.Run(() =>
    {
        var blocking = new BlockingScreen { AllowClose = false };
        var vm = Build(
            [Screen(ScreenKey.Analysis, PermissionType.CanViewReports, factory: _ => blocking)],
            PermissionType.CanViewReports);

        var tab = vm.OpenScreen(ScreenKey.Analysis)!;

        Assert.False(vm.CloseTab(tab));
        Assert.Single(vm.Tabs);
        Assert.Equal(1, blocking.CloseAttempts);

        blocking.AllowClose = true;
        Assert.True(vm.CloseTab(tab));
        Assert.Empty(vm.Tabs);
    });

    // ── Logout sözleşmesi ────────────────────────────────────────────────

    [Fact]
    public void Logout_istegi_disari_tasinir() => ThemeTestHost.Run(() =>
    {
        var vm     = ThreeTabs();
        var raised = 0;
        vm.LogoutRequested += () => raised++;

        Assert.True(vm.RequestLogout());

        Assert.Equal(1, raised);
        Assert.Empty(vm.Tabs);
        Assert.Null(vm.ActiveTab);
    });

    [Fact]
    public void Logout_tum_sekmeleri_kapatir_CanClose_false_dahil() => ThemeTestHost.Run(() =>
    {
        // CanClose yalnızca KULLANICININ kapatmasını engeller; logout'ta kabuk
        // tamamen boşalmalı.
        var vm = Build(
            [Screen(ScreenKey.CashTransactions, PermissionType.CanCreateTransaction, canClose: false)],
            PermissionType.CanCreateTransaction);

        vm.OpenScreen(ScreenKey.CashTransactions);

        Assert.True(vm.RequestLogout());
        Assert.Empty(vm.Tabs);
    });

    [Fact]
    public void Kaydedilmemis_degisiklik_logoutu_iptal_eder() => ThemeTestHost.Run(() =>
    {
        var blocking = new BlockingScreen { AllowClose = false };
        var vm = Build(
            [Screen(ScreenKey.Analysis, PermissionType.CanViewReports, factory: _ => blocking)],
            PermissionType.CanViewReports);

        vm.OpenScreen(ScreenKey.Analysis);

        var raised = 0;
        vm.LogoutRequested += () => raised++;

        Assert.False(vm.RequestLogout());

        // Çıkış yayılmadı ve veri duruyor
        Assert.Equal(0, raised);
        Assert.Single(vm.Tabs);
        Assert.Same(vm.Tabs[0], vm.ActiveTab);
    });

    /// <summary>
    /// Reddeden sekme ortadaysa: öncekiler kapanır, reddeden AKTİF olur ve
    /// sonrakiler açık kalır. Kullanıcı hangi ekranın çıkışı engellediğini
    /// görmeli — sessizce başka bir sekmede bırakılmamalı.
    /// </summary>
    [Fact]
    public void Reddeden_sekme_aktif_yapilir_ve_sonrakiler_acik_kalir() => ThemeTestHost.Run(() =>
    {
        var blocking = new BlockingScreen { AllowClose = false };

        var vm = Build(
        [
            Screen(ScreenKey.CashTransactions, PermissionType.CanCreateTransaction),
            Screen(ScreenKey.Analysis,         PermissionType.CanViewReports, factory: _ => blocking),
            Screen(ScreenKey.AuditLog,         PermissionType.CanViewAuditLog),
        ],
        PermissionType.CanCreateTransaction, PermissionType.CanViewReports, PermissionType.CanViewAuditLog);

        vm.OpenScreen(ScreenKey.CashTransactions);
        vm.OpenScreen(ScreenKey.Analysis);
        vm.OpenScreen(ScreenKey.AuditLog);

        Assert.False(vm.RequestLogout());

        // İlk sekme kapandı, reddeden ve sonrası duruyor
        Assert.Equal(2, vm.Tabs.Count);
        Assert.Equal(ScreenKey.Analysis, vm.Tabs[0].Key);
        Assert.Equal(ScreenKey.AuditLog, vm.Tabs[1].Key);

        // Odak reddeden sekmede
        Assert.NotNull(vm.ActiveTab);
        Assert.Equal(ScreenKey.Analysis, vm.ActiveTab!.Key);
    });

    /// <summary>
    /// Kullanıcı engeli çözünce ikinci deneme başarılı olmalı — iptal kalıcı
    /// bir kilit değil.
    /// </summary>
    [Fact]
    public void Engel_kalkinca_ikinci_logout_denemesi_basarili() => ThemeTestHost.Run(() =>
    {
        var blocking = new BlockingScreen { AllowClose = false };
        var vm = Build(
            [Screen(ScreenKey.Analysis, PermissionType.CanViewReports, factory: _ => blocking)],
            PermissionType.CanViewReports);

        vm.OpenScreen(ScreenKey.Analysis);

        var raised = 0;
        vm.LogoutRequested += () => raised++;

        Assert.False(vm.RequestLogout());
        Assert.Equal(0, raised);

        // Kullanıcı kaydetti / vazgeçti
        blocking.AllowClose = true;

        Assert.True(vm.RequestLogout());
        Assert.Equal(1, raised);
        Assert.Empty(vm.Tabs);
        Assert.Null(vm.ActiveTab);
    });

    /// <summary>
    /// İptal edilen logout ekrana yalnızca BİR kez sormalı; her denemede
    /// kullanıcıya üst üste diyalog çıkmamalı.
    /// </summary>
    [Fact]
    public void Iptal_edilen_logout_ekrana_bir_kez_sorar() => ThemeTestHost.Run(() =>
    {
        var blocking = new BlockingScreen { AllowClose = false };
        var vm = Build(
            [Screen(ScreenKey.Analysis, PermissionType.CanViewReports, factory: _ => blocking)],
            PermissionType.CanViewReports);

        vm.OpenScreen(ScreenKey.Analysis);
        vm.RequestLogout();

        Assert.Equal(1, blocking.CloseAttempts);
    });

    /// <summary>
    /// Pencerenin X'i ile kapatma da aynı korumadan geçer: ShellWindow.OnClosing
    /// CloseAllTabs'ı çağırıp false gelirse kapatmayı iptal eder. Burada o
    /// sözleşmenin ViewModel tarafı doğrulanır.
    /// </summary>
    [Fact]
    public void CloseAllTabs_reddedilirse_false_doner_ve_sekme_durur() => ThemeTestHost.Run(() =>
    {
        var blocking = new BlockingScreen { AllowClose = false };
        var vm = Build(
            [Screen(ScreenKey.Analysis, PermissionType.CanViewReports, factory: _ => blocking)],
            PermissionType.CanViewReports);

        vm.OpenScreen(ScreenKey.Analysis);

        Assert.False(vm.CloseAllTabs());
        Assert.Single(vm.Tabs);
    });

    // ── Kayıt tablosu ────────────────────────────────────────────────────

    [Fact]
    public void Kayit_tablosunda_ScreenKey_tekrari_yok()
    {
        var keys = ScreenRegistry.All.Select(s => s.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    public void Kayit_tablosundaki_ekranlar_henuz_tasinmamis()
    {
        // Faz D adım 2'de hiçbir ekran UserControl'e taşınmadı. Pilot dönüşüm
        // başladığında bu test bilinçli olarak güncellenecek — o zamana kadar
        // kabuğun üretimde boş olduğunu sabitler.
        Assert.All(ScreenRegistry.All, s => Assert.False(s.IsMigrated));
    }

    [Fact]
    public void Kayit_tablosundaki_basliklar_dolu()
    {
        Assert.All(ScreenRegistry.All, s => Assert.False(string.IsNullOrWhiteSpace(s.Title)));
    }
}
