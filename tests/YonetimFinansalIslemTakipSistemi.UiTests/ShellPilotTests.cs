using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.Common;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Shell;
using YonetimFinansalIslemTakipSistemi.UI.Views.Shell;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Finans pilotu: başlangıç akışının kabuğa geçişi (Faz D5).
///
/// NEDEN AYRI DOSYA: <see cref="ShellViewModelTests"/> sekme mantığını
/// ekranlardan bağımsız sınar. Buradaki testler bir adım öteye gider ve
/// GERÇEK <see cref="ShellWindow"/> örneği kurar — varsayılan sekmenin
/// açılması, yetki kapısının pencere kurulumunda da geçerli olması ve
/// kısayolların aktif sekmeye yönlendirilmesi ancak pencere kurulunca
/// görülebilir.
///
/// Ekran listesi pencereye DIŞARIDAN veriliyor: gerçek CashTransactionsScreen
/// tüm veri katmanını isteyeceği için testte sahte ekranlar geçilir. Kabuk
/// hangi ekranın açık olduğunu zaten bilmiyor — sınanan da tam olarak bu.
/// </summary>
public class ShellPilotTests
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

    private sealed class FakeDialogService : IDialogService
    {
        public bool ConfirmationResult { get; set; } = true;
        public int  ConfirmationCount  { get; private set; }

        public void ShowInfo(string message, string title = "Bilgi") { }
        public void ShowSuccess(string message, string title = "Başarılı") { }
        public void ShowWarning(string message, string title = "Uyarı") { }
        public void ShowError(string message, string title = "Hata") { }

        public bool ShowConfirmation(string message, string title = "Onay")
        {
            ConfirmationCount++;
            return ConfirmationResult;
        }
    }

    /// <summary>
    /// Kabuğun kurulumda ihtiyaç duyduğu iki servisi verir; başka bir şey
    /// isterse null döner (bilinçli — kabuk ekranların bağımlılıklarını
    /// çözmemeli).
    /// </summary>
    private sealed class FakeServices(IUserContext userContext, IDialogService dialogService) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IUserContext))   return userContext;
            if (serviceType == typeof(IDialogService)) return dialogService;
            return null;
        }
    }

    /// <summary>Kısayolu gerçekten karşılayan sahte ekran.</summary>
    private sealed class ShortcutScreen : UserControl
    {
        public readonly List<string> Executed = [];

        public ShortcutScreen()
        {
            foreach (var command in AllShortcuts)
            {
                var captured = command;
                CommandBindings.Add(new CommandBinding(
                    captured,
                    (_, _) => Executed.Add(captured.Name)));
            }
        }
    }

    private sealed class BlockingScreen : UserControl, IShellScreen
    {
        public bool AllowClose { get; set; }
        public bool RequestClose() => AllowClose;
    }

    private static readonly RoutedUICommand[] AllShortcuts =
    [
        AppCommands.New,
        AppCommands.Duplicate,
        AppCommands.DeleteSelected,
        AppCommands.FocusSearch,
        AppCommands.ImportExcel,
        AppCommands.RefreshList,
    ];

    /// <summary>Nakit İşlemler'in gerçek yetki kümesi (ScreenRegistry ile aynı).</summary>
    private static readonly PermissionType[] FinanceAccess =
    [
        PermissionType.CanCreateTransaction,
        PermissionType.CanEditTransaction,
        PermissionType.CanDeleteTransaction,
        PermissionType.CanViewReports,
        PermissionType.CanManageUsers,
        PermissionType.CanViewAuditLog,
        PermissionType.CanManageExchangeRates,
    ];

    private static ScreenDefinition CashScreen(Func<IServiceProvider, FrameworkElement> factory)
        => new(ScreenKey.CashTransactions, "Nakit İşlemler", FinanceAccess,
               CreateView: factory, CreateInstance: null,
               IsParameterized: false, CanClose: false);

    private static ShellWindow BuildShell(
        IReadOnlyList<ScreenDefinition> screens,
        IDialogService?                 dialogService = null,
        params PermissionType[]         permissions)
        => new(new FakeServices(new FakeUserContext(permissions), dialogService ?? new FakeDialogService()),
               screens);

    private static ShellViewModel VmOf(ShellWindow window) => (ShellViewModel)window.DataContext;

    // ── Varsayılan sekme ─────────────────────────────────────────────────

    [Fact]
    public void Acilista_tek_Nakit_Islemler_sekmesi_olusur() => ThemeTestHost.Run(() =>
    {
        var shell = BuildShell([CashScreen(_ => new UserControl())],
                               dialogService: null,
                               PermissionType.CanCreateTransaction);

        var vm = VmOf(shell);

        Assert.Single(vm.Tabs);
        Assert.Equal(ScreenKey.CashTransactions, vm.Tabs[0].Key);
        Assert.Same(vm.Tabs[0], vm.ActiveTab);
    });

    /// <summary>
    /// Navigasyondan aynı ekran tekrar seçilirse ikinci sekme oluşmaz;
    /// mevcut sekmeye odaklanılır.
    /// </summary>
    [Fact]
    public void Ayni_ekran_ikinci_sekme_uretmez() => ThemeTestHost.Run(() =>
    {
        var shell = BuildShell([CashScreen(_ => new UserControl())],
                               dialogService: null,
                               PermissionType.CanCreateTransaction);

        var vm    = VmOf(shell);
        var first = vm.Tabs[0];

        vm.OpenScreen(ScreenKey.CashTransactions);
        vm.OpenScreen(ScreenKey.CashTransactions);

        Assert.Single(vm.Tabs);
        Assert.Same(first, vm.Tabs[0]);
        Assert.Same(first, vm.ActiveTab);
    });

    /// <summary>
    /// Varsayılan sekme AÇILIRKEN de yetki kapısı geçerli. Yetkisiz kullanıcı
    /// kabuğu görebilir (teoride startup buraya yönlendirmez) ama ekranı
    /// açamaz — navigasyonda gizlemek tek başına yeterli değil.
    /// </summary>
    [Fact]
    public void Yetkisiz_kullanici_Nakit_Islemler_sekmesi_alamaz() => ThemeTestHost.Run(() =>
    {
        var shell = BuildShell([CashScreen(_ => new UserControl())],
                               dialogService: null,
                               PermissionType.CanViewCargoModule);

        var vm = VmOf(shell);

        Assert.Empty(vm.Tabs);
        Assert.Empty(vm.NavigationItems);

        // Programatik deneme de reddedilir
        Assert.Null(vm.OpenScreen(ScreenKey.CashTransactions));
        Assert.Empty(vm.Tabs);
    });

    /// <summary>
    /// Taşınmamış ekran navigasyonda GÖRÜNMEZ — tıklandığında hiçbir şey
    /// yapmayan sahte sekme üretilmemeli.
    /// </summary>
    [Fact]
    public void Tasinmamis_ekran_navigasyonda_gorunmez() => ThemeTestHost.Run(() =>
    {
        var notMigrated = new ScreenDefinition(
            ScreenKey.Reports, "Raporlar", [PermissionType.CanViewReports]);

        var shell = BuildShell([CashScreen(_ => new UserControl()), notMigrated],
                               dialogService: null,
                               PermissionType.CanCreateTransaction, PermissionType.CanViewReports);

        var vm = VmOf(shell);

        Assert.Single(vm.NavigationItems);
        Assert.Equal(ScreenKey.CashTransactions, vm.NavigationItems[0].Key);
        Assert.Null(vm.OpenScreen(ScreenKey.Reports));
    });

    // ── Kısayollar ───────────────────────────────────────────────────────

    /// <summary>
    /// Altı kısayol da AKTİF SEKMEYE ulaşır — odak sekmenin dışındayken bile.
    ///
    /// Komut pencereden başlatılır (odak navigasyon rayındaymış gibi); kabuk
    /// onu aktif ekrana yönlendirir. Kabuk komutun ne yaptığını bilmez,
    /// yalnızca hedefi değiştirir.
    /// </summary>
    [Fact]
    public void Alti_kisayol_aktif_sekmeye_gider() => ThemeTestHost.Run(() =>
    {
        var screen = new ShortcutScreen();
        var shell  = BuildShell([CashScreen(_ => screen)],
                                dialogService: null,
                                PermissionType.CanCreateTransaction);

        foreach (var command in AllShortcuts)
            command.Execute(null, shell);

        Assert.Equal(
            AllShortcuts.Select(c => c.Name).ToArray(),
            screen.Executed.ToArray());
    });

    /// <summary>
    /// Aktif sekme yönlendirmeyi karşılamıyorsa kabuk sessizce durur —
    /// komut ağaçta tekrar pencereye yükselip sonsuz döngü kurmamalı.
    /// </summary>
    [Fact]
    public void Karsiliksiz_kisayol_sonsuz_donguye_girmez() => ThemeTestHost.Run(() =>
    {
        var shell = BuildShell([CashScreen(_ => new UserControl())],
                               dialogService: null,
                               PermissionType.CanCreateTransaction);

        // Sekme içeriğinin hiçbir CommandBinding'i yok; çağrı geri dönmeli
        AppCommands.New.Execute(null, shell);
    });

    // ── Çıkış ────────────────────────────────────────────────────────────

    /// <summary>
    /// Onay sekmeler kapatılmadan ÖNCE sorulur. Kullanıcı vazgeçerse kabuk
    /// sekmeleri açık kalır — aksi hâlde iptal edilen bir çıkış kabuğu boş
    /// bırakırdı.
    /// </summary>
    [Fact]
    public void Iptal_edilen_cikis_sekmeleri_kapatmaz() => ThemeTestHost.Run(() =>
    {
        var dialogs = new FakeDialogService { ConfirmationResult = false };
        var shell   = BuildShell([CashScreen(_ => new UserControl())],
                                 dialogs,
                                 PermissionType.CanCreateTransaction);

        var vm = VmOf(shell);

        Assert.False(vm.RequestLogout());
        Assert.Equal(1, dialogs.ConfirmationCount);
        Assert.Single(vm.Tabs);
        Assert.False(shell.IsLogoutRequested);
    });

    /// <summary>
    /// Onay verilse bile bir ekran kapanmayı reddederse çıkış iptal edilir.
    /// </summary>
    [Fact]
    public void Ekran_reddederse_cikis_iptal_edilir() => ThemeTestHost.Run(() =>
    {
        var blocking = new BlockingScreen { AllowClose = false };
        var dialogs  = new FakeDialogService { ConfirmationResult = true };
        var shell    = BuildShell([CashScreen(_ => blocking)],
                                  dialogs,
                                  PermissionType.CanCreateTransaction);

        var vm = VmOf(shell);

        Assert.False(vm.RequestLogout());
        Assert.Single(vm.Tabs);
        Assert.False(shell.IsLogoutRequested);
    });

    /// <summary>
    /// Ekranın kendi araç çubuğundaki çıkış düğmesi kabuk içinde de aynı
    /// akışa çıkar — MainWindow dışında ölü kalmamalı.
    /// </summary>
    [Fact]
    public void Ekran_ici_cikis_dugmesi_kabuk_akisina_baglanir() => ThemeTestHost.Run(() =>
    {
        var screen  = new LogoutScreen();
        var dialogs = new FakeDialogService { ConfirmationResult = false };
        var shell   = BuildShell([CashScreen(_ => screen)],
                                 dialogs,
                                 PermissionType.CanCreateTransaction);

        screen.RaiseLogout();

        // Onay sorulduysa istek kabuğa ulaşmış demektir
        Assert.Equal(1, dialogs.ConfirmationCount);
    });

    private sealed class LogoutScreen : UserControl, IShellLogoutSource
    {
        public event Action? LogoutRequested;
        public void RaiseLogout() => LogoutRequested?.Invoke();
    }

    // ── Kaynak sözleşmeleri ──────────────────────────────────────────────

    private static string ShellWindowCode =>
        File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
            "Views", "Shell", "ShellWindow.xaml.cs"), Encoding.UTF8);

    private static string ShellWindowMarkup =>
        Regex.Replace(
            File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
                "Views", "Shell", "ShellWindow.xaml"), Encoding.UTF8),
            @"<!--.*?-->", string.Empty, RegexOptions.Singleline);

    /// <summary>
    /// Her kayıtlı ekranın fabrikası dolu ve doğasına uygun olmalı: tekil
    /// ekran <c>CreateView</c>, parametreli ekran <c>CreateInstance</c>.
    /// </summary>
    [Fact]
    public void Her_ekranin_fabrikasi_dogasina_uygun()
    {
        var wrong = ScreenRegistry.All
            .Where(s => s.IsParameterized ? s.CreateInstance is null : s.CreateView is null)
            .Select(s => s.Key)
            .ToList();

        Assert.True(wrong.Count == 0,
            "Fabrikası eksik/yanlış türde ekran(lar): " + string.Join(", ", wrong));
    }

    /// <summary>
    /// Kabuk kurulumda varsayılan sekmeyi açar.
    ///
    /// EKRAN yetkisi burada TEKRARLANMAZ — tek kapı ShellViewModel.Resolve.
    /// Kabuktaki tek yetki kontrolü ekran AÇMAYAN araç düğmeleri içindir
    /// (ApplyToolVisibility); onların kaydı ScreenRegistry'de yok.
    /// </summary>
    [Fact]
    public void Kabuk_varsayilan_sekmeyi_aciyor()
    {
        Assert.Contains("OpenScreen(ScreenKey.CashTransactions)", ShellWindowCode, StringComparison.Ordinal);

        // Ekran yetkisi kabukta çözülmemeli: ScreenRegistry'ye bakan bir
        // yetki filtresi kabuk kodunda olmamalı.
        Assert.DoesNotContain("IsAllowedFor", ShellWindowCode, StringComparison.Ordinal);
        Assert.DoesNotContain("RequiredPermissions", ShellWindowCode, StringComparison.Ordinal);
    }

    /// <summary>
    /// Kısayol tuşları kabukta tanımlı ve hepsi yönlendirmeye bağlı.
    /// Kabuk hiçbir ekranın metodunu doğrudan çağırmamalı.
    /// </summary>
    [Theory]
    [InlineData("New")]
    [InlineData("Duplicate")]
    [InlineData("DeleteSelected")]
    [InlineData("FocusSearch")]
    [InlineData("ImportExcel")]
    [InlineData("RefreshList")]
    public void Kabukta_kisayol_tanimli(string command)
    {
        var markup = ShellWindowMarkup;

        Assert.Contains($@"<KeyBinding Key=", markup, StringComparison.Ordinal);
        Assert.Matches(
            $@"<CommandBinding\s+Command=""common:AppCommands\.{command}""\s+Executed=""Command_Forward""",
            markup);
    }

    /// <summary>
    /// Kabuk hiçbir ekran türünü tanımıyor. Tanısaydı her yeni ekran için
    /// buraya kod eklemek gerekirdi ve "tek kabuk" iddiası bozulurdu.
    /// </summary>
    [Fact]
    public void Kabuk_ekran_turlerini_tanimiyor()
    {
        Assert.DoesNotContain("CashTransactionsScreen", ShellWindowCode, StringComparison.Ordinal);
        Assert.DoesNotContain("CashTransactionListViewModel", ShellWindowCode, StringComparison.Ordinal);
    }

    /// <summary>
    /// Çıkış sözleşmesi App.xaml.cs ile uyumlu: denetim kaydı yazılır, sonra
    /// bayrak ve kapatma. MainWindow ile aynı sıra.
    /// </summary>
    [Fact]
    public void Kabuk_cikis_sozlesmesini_uyguluyor()
    {
        var code = ShellWindowCode;

        Assert.Contains("SessionLogout.WriteAuditAsync", code, StringComparison.Ordinal);
        Assert.Contains("IsLogoutRequested = true;", code, StringComparison.Ordinal);
        Assert.Contains("Close();", code, StringComparison.Ordinal);
    }

    /// <summary>
    /// UserLoggedOut denetim kaydı tek yerde. Üçüncü kabuk penceresi eklenince
    /// kopyalanmamalı — kopyalanırsa biri düzeltildiğinde diğeri geride kalır.
    /// </summary>
    [Fact]
    public void Cikis_audit_kaydi_tek_yerde()
    {
        var writers = UiSourceLocator.CsFiles()
            .Where(p => File.ReadAllText(p, Encoding.UTF8).Contains("AuditAction.UserLoggedOut", StringComparison.Ordinal))
            .Select(UiSourceLocator.Relative)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        // CargoDashboardWindow kendi kopyasını taşımaya devam ediyor (kapsam
        // dışı); yeni kabuk ise ortak yardımcıyı kullanır.
        Assert.DoesNotContain("MainWindow.xaml.cs", writers);
        Assert.DoesNotContain("Views/Shell/ShellWindow.xaml.cs", writers);
        Assert.Contains("Common/SessionLogout.cs", writers);
    }

    /// <summary>
    /// Harf Duyarlılığı finans kabuğundan erişilebilir olmalı — kişisel ayar,
    /// yetki gerektirmez ve kabuğa geçişte kaybolmamalı.
    /// </summary>
    [Fact]
    public void Harf_duyarliligi_kabuktan_erisilebilir()
    {
        Assert.Contains("Harf Duyarlılığı", ShellWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("OpenTextCaseSettings_Click", ShellWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("TextCaseSettingsWindow", ShellWindowCode, StringComparison.Ordinal);
    }

    /// <summary>
    /// MainWindow silinmedi ve hâlâ derlenebilir durumda — geri dönüş yolu
    /// açık. Ama başlangıç akışı onu açmıyor (bkz. ShellStartupContractTests).
    /// </summary>
    [Fact]
    public void MainWindow_kodda_duruyor()
    {
        Assert.True(File.Exists(Path.Combine(UiSourceLocator.UiProjectDirectory, "MainWindow.xaml")));
        Assert.True(File.Exists(Path.Combine(UiSourceLocator.UiProjectDirectory, "MainWindow.xaml.cs")));
        Assert.NotNull(typeof(MainWindow));
    }

    // ── Başlangıç kararı ─────────────────────────────────────────────────
    //
    // Kaynak taraması hangi pencerenin açıldığını gösterir; KARARIN kendisi
    // ancak çalıştırılarak doğrulanır.

    [Theory]
    [InlineData(PermissionType.CanCreateTransaction,   "finance")]
    [InlineData(PermissionType.CanViewReports,         "finance")]
    [InlineData(PermissionType.CanManageUsers,         "finance")]
    [InlineData(PermissionType.CanManageExchangeRates, "finance")]
    [InlineData(PermissionType.CanViewCargoModule,     "cargo")]
    [InlineData(PermissionType.CanViewIncomingCargo,   "cargo")]
    [InlineData(PermissionType.CanManageCargoCompanies, "cargo")]
    [InlineData(PermissionType.CanAccessSettings,      "none")]
    public void Baslangic_karari_degismedi(PermissionType permission, string expected)
    {
        Assert.Equal(expected, App.ResolveStartupMode(new FakeUserContext(permission)));
    }

    /// <summary>
    /// Finans + kargo yetkisi birlikteyse finans kazanır — mevcut davranış.
    /// </summary>
    [Fact]
    public void Finans_ve_kargo_birlikteyse_finans_kazanir()
    {
        var mode = App.ResolveStartupMode(
            new FakeUserContext(PermissionType.CanViewReports, PermissionType.CanViewCargoModule));

        Assert.Equal("finance", mode);
    }

    /// <summary>
    /// Hiç yetkisi olmayan kullanıcı "none" — pencere açılmaz.
    /// </summary>
    [Fact]
    public void Yetkisiz_kullanici_none()
    {
        Assert.Equal("none", App.ResolveStartupMode(new FakeUserContext()));
    }
}
