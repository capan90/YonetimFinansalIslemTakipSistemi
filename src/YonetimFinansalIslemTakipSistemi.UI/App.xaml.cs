using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.AuditLogs.Queries.GetAuditLogs;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.CreateCashTransaction;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.DeleteCashTransaction;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.UpdateCashTransaction;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCashTransactions;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCurrentBalances;
using YonetimFinansalIslemTakipSistemi.Application.Features.Users.Commands.CreateUser;
using YonetimFinansalIslemTakipSistemi.Application.Features.Users.Commands.DeleteUser;
using YonetimFinansalIslemTakipSistemi.Application.Features.Users.Commands.UpdateUser;
using YonetimFinansalIslemTakipSistemi.Application.Features.Permissions.Commands.UpdateUserPermissions;
using YonetimFinansalIslemTakipSistemi.Application.Features.Reports.Queries.GetReport;
using YonetimFinansalIslemTakipSistemi.Application.Features.Permissions.Queries.GetUserPermissions;
using YonetimFinansalIslemTakipSistemi.Application.Features.ExchangeRates.Commands.CreateOrUpdateExchangeRate;
using YonetimFinansalIslemTakipSistemi.Application.Features.ExchangeRates.Commands.FetchTcmbExchangeRates;
using YonetimFinansalIslemTakipSistemi.Application.Features.ExchangeRates.Queries.GetExchangeRates;
using YonetimFinansalIslemTakipSistemi.Application.Features.Users.Queries.GetUsers;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Infrastructure;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Services;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.Services;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.AuditLogs;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Login;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Permissions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Reports;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Users;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Analysis;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.ExchangeRates;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Commands.CreateCompanyDirectory;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Commands.UpdateCompanyDirectory;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Commands.DeleteCompanyDirectory;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Queries.GetCompanyDirectoryList;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyAttentionContacts.GetCompanyAttentionContacts;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyAttentionContacts.EnsureCompanyAttentionContact;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Commands.CreateCargoCompany;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Commands.UpdateCargoCompany;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Commands.DeleteCargoCompany;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Queries.GetCargoCompanyList;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.CreateCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.UpdateCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.DeleteCargoShipment;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.QuickUpdateCargoStatus;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Import;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Import;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.Import;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Label.GenerateCargoLabel;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification.GenerateCargoNotification;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification.MarkCargoNotificationPrepared;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoDashboard;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoReport;
using YonetimFinansalIslemTakipSistemi.Application.Features.Settings.MailSettings;
using YonetimFinansalIslemTakipSistemi.Application.Features.Settings.UserPreferences;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.CreateWhatsAppContact;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.DeleteWhatsAppContact;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.GetWhatsAppContactList;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.UpdateWhatsAppContact;
using YonetimFinansalIslemTakipSistemi.Application.Services;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.WhatsApp;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;
using YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;
using YonetimFinansalIslemTakipSistemi.UI.Views.SystemLogs;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.SystemLogs;

namespace YonetimFinansalIslemTakipSistemi.UI;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Çözümlenmiş log klasörü — MainWindow "Log Klasörünü Aç" için kullanır.
    /// </summary>
    public static string LogDirectory { get; private set; } = string.Empty;

    // ClickOnce uygulaması her sürümde farklı versiyonlu klasörden (%LocalAppData%\Apps\2.0\<hash>\...)
    // çalışır. Görev çubuğu, sabit bir AppUserModelID atanmazsa pencereyi bu değişken exe yoluyla
    // gruplar; güncelleme sonrası yol değişince kullanıcının sabitlediği (pinlediği) simge kırılır ve
    // uygulama ayrı bir simge olarak açılır. Sabit AUMID bu gruplamayı sürümden bağımsız kılar.
    private const string AppUserModelId = "ErdemSoftTekstil.YonetimFinansalIslemTakip";

    [System.Runtime.InteropServices.DllImport("shell32.dll", PreserveSig = false)]
    private static extern void SetCurrentProcessExplicitAppUserModelID(
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string appID);

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Görev çubuğu kimliği ilk pencereden ÖNCE sabitlenmeli (sonra atamak etkisiz kalır).
        // Başarısız olursa uygulama akışı engellenmez — yalnızca pin gruplaması iyileştirmesidir.
        try { SetCurrentProcessExplicitAppUserModelID(AppUserModelId); }
        catch (Exception ex) { Log.Warning(ex, "AppUserModelID ayarlanamadı (pin gruplaması)"); }

        RegisterGlobalKeyBindings();

        // Global handler'lar logger kurulmadan önce kaydedilir.
        // Logger henüz hazır değilse Serilog'un varsayılan sessiz logger'ı devreye girer.
        AppDomain.CurrentDomain.UnhandledException  += OnUnhandledException;
        Current.DispatcherUnhandledException        += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException       += OnUnobservedTaskException;

        try
        {
            await InitializeAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Uygulama başlatılamadı");
            // Services henüz hazır olabilir (DB testi sonrası) — bildirimi dene
            SafeLog(s => s.LogCriticalAsync("Startup", "Uygulama başlatılamadı — veritabanı bağlantısı kurulamadı", ex, source: "App.OnStartup"));
            Log.CloseAndFlush();
            ShowConnectionError();
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Uygulama kapatılıyor");
        SafeLog(s => s.LogInfoAsync("Startup", "Uygulama kapatıldı", source: "App.OnExit"));
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private async Task InitializeAsync()
    {
        var config = BuildConfiguration();

        // ClickOnce güncelleme yolunu yapılandır: env var > appsettings Deployment:UpdatePath > üretim varsayılanı.
        // ProviderURL ile aynı UNC olmalı; aksi halde manuel güncelleme kontrolü yanlış sunucuya bakar.
        DeploymentSettings.Configure(config["Deployment:UpdatePath"]);

        // Logger'ı bağlantı testi ve hata mesajlarından önce kur
        LogDirectory = ResolveLogDirectory(config);
        Log.Logger   = CreateLogger(config, LogDirectory);

        var appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        Log.Information("Uygulama başlatılıyor. Sürüm: {AppVersion}, Makine: {MachineName}",
            appVersion, Environment.MachineName);

        // Öncelik: env var > appsettings.json > hata
        var connectionString =
            Environment.GetEnvironmentVariable("YONETIM_DB_CONNECTION")
            ?? config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Bağlantı dizesi yapılandırılmamış.");

        // Aktif ortam, BuildConfiguration ile aynı önceliği kullanır (env var > config > Development).
        var appEnvironment = ResolveEnvironmentName(config["AppEnvironment"]);
        var isDevelopment  = !appEnvironment.Equals("Production", StringComparison.OrdinalIgnoreCase);

        Log.Debug("Ortam: {AppEnvironment}", appEnvironment);

        var services = new ServiceCollection();
        // IConfiguration'ı DI'a al — AesSecretProtector ve diğer servisler için
        services.AddSingleton<IConfiguration>(config);
        // EF SQL komut logu seviyesi: varsayılan Debug (gizli, log şişmez). SQL görmek için
        // appsettings "Logging:EfCommandLevel" = "Information" yapılır.
        services.AddInfrastructure(connectionString, ParseEfCommandLevel(config["Logging:EfCommandLevel"]));

        // SMTP bildirimi: env var YONETIM_SMTP_PASSWORD / YONETIM_SMTP_USERNAME şifre güvenliği sağlar
        var smtpOptions = BuildSmtpNotificationOptions(config);
        services.AddSingleton(smtpOptions);
        // Son kayıt önceliklidir — AddInfrastructure'daki NullErrorNotificationService'in üzerine yazar
        services.AddSingleton<IErrorNotificationService, SmtpErrorNotificationService>();

        // HealthCheckService için ortam bilgileri — UI katmanı bu nesneyi oluşturur
        // çünkü DeploymentSettings ve App.LogDirectory burada erişilebilir.
        services.AddSingleton(new HealthCheckOptions
        {
            AppEnvironment         = appEnvironment,
            LogDirectory           = LogDirectory,
            BackupDirectory        = ResolveBackupDirectory(config),
            UpdatePublishPath      = DeploymentSettings.PublishPath,
            ConnectionString       = connectionString,
            NotificationsEnabled   = smtpOptions.Enabled,
            NotificationProvider   = smtpOptions.Provider,
            NotificationToConfigured = !string.IsNullOrEmpty(smtpOptions.To),
            NotificationSmtpHost   = smtpOptions.SmtpHost,
            // Kullanıcı adı VE şifre her ikisi de ayarlıysa true; değerler Health Check ekranında gösterilmez
            NotificationCredentialsConfigured =
                !string.IsNullOrEmpty(smtpOptions.SmtpUsername) && !string.IsNullOrEmpty(smtpOptions.SmtpPassword)
        });

        // Serilog → Microsoft.Extensions.Logging köprüsü:
        // Infrastructure servisleri (ör. ReportExportService) ILogger<T> üzerinden yazabilir.
        services.AddLogging(lb => lb.AddSerilog(dispose: false));

        // Dialog servisi — singleton: durumsuz, her çağrıda yeni pencere nesnesi oluşturur
        services.AddSingleton<IDialogService, DialogService>();

        // Güncelleme servisi — singleton: UNC dosya okuma, durumsuz
        services.AddSingleton<IUpdateService, UpdateService>();

        // Oturum bağlamı — tek singleton örneği iki arayüz üzerinden açılır:
        //   IUserContext (okuma): handler'lar ve VM'ler kullanır
        //   IUserSession (yazma): LoginViewModel.SetUser(), App.xaml.cs.Clear()
        var userContext = new UserContext();
        services.AddSingleton(userContext);
        services.AddSingleton<IUserContext>(userContext);
        services.AddSingleton<IUserSession>(userContext);

        // CashTransaction handler'lar
        services.AddScoped<CreateCashTransactionHandler>();
        services.AddScoped<UpdateCashTransactionHandler>();
        services.AddScoped<DeleteCashTransactionHandler>();
        services.AddScoped<GetCashTransactionsHandler>();
        services.AddScoped<GetCurrentBalancesHandler>();

        // User handler'lar
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<UpdateUserHandler>();
        services.AddScoped<DeleteUserHandler>();
        services.AddScoped<GetUsersHandler>();

        // Audit log handler
        services.AddScoped<GetAuditLogsHandler>();

        // Permissions handler'lar
        services.AddScoped<GetUserPermissionsHandler>();
        services.AddScoped<UpdateUserPermissionsHandler>();

        // Report handler
        services.AddScoped<GetReportHandler>();

        // ExchangeRate handler'lar
        services.AddScoped<CreateOrUpdateExchangeRateHandler>();
        services.AddScoped<GetExchangeRatesHandler>();
        // TCMB'den çekim — kaynağı çağırıp upsert handler'ıyla kaydeder
        services.AddScoped<FetchTcmbExchangeRatesHandler>();

        // Kargo Katip handler'lar
        services.AddScoped<CreateCompanyDirectoryHandler>();
        services.AddScoped<UpdateCompanyDirectoryHandler>();
        services.AddScoped<DeleteCompanyDirectoryHandler>();
        services.AddScoped<GetCompanyDirectoryListHandler>();
        services.AddScoped<GetCompanyAttentionContactsHandler>();
        services.AddScoped<EnsureCompanyAttentionContactHandler>();
        // Gönderi/teslim isim önerileri — geçmiş kargo kayıtlarından türetilir
        services.AddScoped<Application.Features.CargoShipment.Queries.GetCargoPartySuggestions.GetCargoPartySuggestionsHandler>();
        services.AddScoped<CreateCargoCompanyHandler>();
        services.AddScoped<UpdateCargoCompanyHandler>();
        services.AddScoped<DeleteCargoCompanyHandler>();
        services.AddScoped<GetCargoCompanyListHandler>();
        services.AddScoped<CreateCargoShipmentHandler>();
        services.AddScoped<UpdateCargoShipmentHandler>();
        services.AddScoped<DeleteCargoShipmentHandler>();
        services.AddScoped<QuickUpdateCargoStatusHandler>();
        services.AddScoped<GetCargoShipmentListHandler>();
        // Excel içe aktarma sihirbazları (kargo gönderi / firma rehberi / WhatsApp rehberi)
        services.AddScoped<AnalyzeCargoImportHandler>();
        services.AddScoped<ImportCargoShipmentsHandler>();
        services.AddScoped<AnalyzeDirectoryImportHandler>();
        services.AddScoped<ImportDirectoryEntriesHandler>();
        services.AddScoped<AnalyzeWhatsAppImportHandler>();
        services.AddScoped<ImportWhatsAppContactsHandler>();
        services.AddScoped<AnalyzeCashImportHandler>();
        services.AddScoped<ImportCashTransactionsHandler>();
        services.AddTransient<ViewModels.Import.DirectoryImportViewModel>();
        services.AddTransient<ViewModels.Import.WhatsAppImportViewModel>();
        services.AddTransient<ViewModels.Import.CashImportViewModel>();
        services.AddScoped<GenerateCargoLabelHandler>();
        // ILabelRenderer: Singleton — renderer durumsuz, her session paylaşabilir
        services.AddSingleton<ILabelRenderer, QuestPdfLabelRenderer>();
        // ICompanyInfoProvider — AppSettings'ten gönderici firma bilgisi; ileride Company Settings modülüyle değiştirilebilir
        services.AddSingleton<ICompanyInfoProvider>(new AppSettingsCompanyInfoProvider(config));

        // Kargo mail gönderimi — SMTP ayarları artık DB'den (IMailSettingsService) okunur
        services.AddSingleton<ICargoMailSenderService, CargoSmtpMailSenderService>();

        // Bildirim — Sprint 3.4/3.5 (WhatsApp + SMTP Mail)
        services.AddSingleton<WhatsAppNotificationComposer>();
        services.AddSingleton<MailNotificationComposer>();
        services.AddScoped<GenerateCargoNotificationHandler>();
        services.AddScoped<MarkCargoNotificationPreparedHandler>();

        // Dashboard + Rapor — Sprint 5
        services.AddScoped<GetCargoDashboardHandler>();
        services.AddScoped<GetCargoReportHandler>();
        // ICargoReportPdfExporter: Singleton — renderer durumsuz
        services.AddSingleton<ICargoReportPdfExporter, QuestPdfCargoReportExporter>();

        // Mail Ayarları — Sprint 6
        services.AddScoped<GetMailSettingsHandler>();
        services.AddScoped<SaveMailSettingsHandler>();
        services.AddScoped<SendTestMailHandler>();

        // Kullanıcı tercihleri (harf duyarlılığı) — merkezi normalize servisi tercihi IUserContext'ten okur
        services.AddSingleton<IUserTextNormalizationService, UserTextNormalizationService>();
        services.AddScoped<GetUserPreferenceHandler>();
        services.AddScoped<SaveUserPreferenceHandler>();

        // Ortak WhatsApp rehberi
        services.AddScoped<GetWhatsAppContactListHandler>();
        services.AddScoped<CreateWhatsAppContactHandler>();
        services.AddScoped<UpdateWhatsAppContactHandler>();
        services.AddScoped<DeleteWhatsAppContactHandler>();

        // Ortak mail rehberi — mail bildirim ekranındaki alıcı/CC seçimi
        services.AddScoped<Application.Features.MailContacts.GetMailContactList.GetMailContactListHandler>();
        services.AddScoped<Application.Features.MailContacts.CreateMailContact.CreateMailContactHandler>();
        services.AddScoped<Application.Features.MailContacts.UpdateMailContact.UpdateMailContactHandler>();
        services.AddScoped<Application.Features.MailContacts.DeleteMailContact.DeleteMailContactHandler>();
        services.AddScoped<Application.Features.MailContacts.TouchMailContacts.TouchMailContactsHandler>();
        services.AddTransient<ViewModels.Mail.MailContactListViewModel>();
        services.AddTransient<ViewModels.Mail.MailContactEditViewModel>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<CashTransactionListViewModel>();
        services.AddTransient<CashTransactionFormViewModel>();
        services.AddTransient<UserManagementViewModel>();
        services.AddTransient<UserFormViewModel>();
        services.AddTransient<AuditLogViewModel>();
        services.AddTransient<UserPermissionViewModel>();
        services.AddTransient<ReportViewModel>();
        services.AddTransient<ExchangeRateViewModel>();
        services.AddTransient<AnalysisViewModel>();

        // Sistem Logları modülü
        services.AddTransient<SystemLogsViewModel>();

        // Tema servisi — UI katmanında (WPF Application.Current erişimi gerektirir)
        services.AddSingleton<IThemeService, YonetimFinansalIslemTakipSistemi.UI.Services.ThemeService>();

        // Lokal kullanıcı tercihleri — makine başına %AppData% dosyasına yazılır, DB kullanılmaz
        services.AddSingleton<ILocalUserPreferencesService, LocalUserPreferencesService>();

        // Kargo Katip ViewModels
        services.AddTransient<CompanyDirectoryListViewModel>();
        services.AddTransient<CompanyDirectoryEditViewModel>();
        services.AddTransient<CargoCompanyListViewModel>();
        services.AddTransient<CargoCompanyEditViewModel>();
        services.AddTransient<CargoShipmentEditViewModel>();
        services.AddTransient<WhatsAppContactListViewModel>();
        services.AddTransient<WhatsAppContactEditViewModel>();

        Services = services.BuildServiceProvider();

        // Veritabanı bağlantısını erken test et — hata varsa kullanıcı dostu mesaj göster
        using (var testScope = Services.CreateScope())
        {
            var testService = testScope.ServiceProvider.GetRequiredService<IDatabaseConnectionTestService>();
            if (!await testService.CanConnectAsync())
            {
                Log.Error("Veritabanına bağlanılamadı — startup iptal ediliyor");
                // SystemLogService henüz hazır olabilir — yazma denemesi yap
                SafeLog(s => s.LogErrorAsync("Database", "Veritabanına bağlanılamadı", source: "Startup"));
                throw new InvalidOperationException("Veritabanına bağlanılamadı.");
            }
        }

        Log.Information("Veritabanı bağlantısı doğrulandı");
        SafeLog(s => s.LogInfoAsync("Database", "Veritabanı bağlantısı doğrulandı", source: "Startup"));

        // Bekleyen EF Core migration'larını otomatik uygula
        using (var migrationScope = Services.CreateScope())
        {
            await migrationScope.ServiceProvider
                .GetRequiredService<DatabaseMigrator>()
                .ApplyAsync();
            Log.Information("Veritabanı migration'ları uygulandı");
        }

        // appsettings.json SMTP ayarlarını DB'ye ilk çalıştırmada ekle
        using (var seedMailScope = Services.CreateScope())
        {
            var seeder = seedMailScope.ServiceProvider.GetRequiredService<MailSettingsSeeder>();
            var smtp   = Services.GetRequiredService<SmtpNotificationOptions>();
            await seeder.SeedAsync(smtp);
            Log.Information("Mail ayarları kontrol edildi");
        }

        // [DEV-ONLY] Seed yalnızca geliştirme ortamında çalışır
        if (isDevelopment)
        {
            using var seedScope = Services.CreateScope();
            await seedScope.ServiceProvider.GetRequiredService<IDevDataSeeder>().SeedAsync();
        }

        // Kaydedilmiş temayı DB'den oku ve uygula (login öncesi, UI thread'inde)
        var themeService = Services.GetRequiredService<IThemeService>();
        var savedTheme   = await themeService.GetCurrentThemeAsync();
        themeService.ApplyTheme(savedTheme);

        Log.Information("Uygulama başlatıldı, oturum döngüsü başlıyor");
        SafeLog(s => s.LogInfoAsync("Startup", "Uygulama başlatıldı", source: "App.InitializeAsync"));

        // Kargo retention arka planda çalışsın — UI'ı bloklamamak için fire-and-forget
        _ = Services.GetRequiredService<ICargoRetentionService>().RunAsync();

        RunApplicationLoop();
    }

    // ── Global Exception Handlers ────────────────────────────────────────────

    /// <summary>
    /// Esc = pencereyi kapat — sınıf düzeyinde, tüm pencereler için bir kez.
    /// 24 pencerenin her birine ayrı KeyBinding + CommandBinding eklemek yerine
    /// Window tipine kaydediliyor; yeni eklenen pencereler de otomatik kapsanır.
    ///
    /// ComboBox açıkken veya hücre düzenlemesindeyken Esc'i o kontroller önce
    /// işleyip "handled" işaretler — pencere kapanmaz, beklenen davranış korunur.
    /// İptal butonu olan diyaloglarda IsCancel zaten aynı sonucu veriyordu.
    /// </summary>
    private static void RegisterGlobalKeyBindings()
    {
        CommandManager.RegisterClassCommandBinding(
            typeof(Window),
            new CommandBinding(
                UI.Common.AppCommands.CloseWindow,
                (sender, _) => (sender as Window)?.Close()));

        CommandManager.RegisterClassInputBinding(
            typeof(Window),
            new KeyBinding(UI.Common.AppCommands.CloseWindow, Key.Escape, ModifierKeys.None));
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;

        // Önce dosya: Serilog kapatıldıktan sonra ya da log altyapısı hiç
        // kurulamadığında bile hatanın izi kalmalı.
        WriteCrashFile(ex, $"Yakalanmamış AppDomain istisnası (IsTerminating={e.IsTerminating})");

        Log.Fatal(ex, "Yakalanmamış uygulama istisnası (AppDomain). Kapanıyor: {IsTerminating}", e.IsTerminating);
        // SystemLogService kritik log yazar ve içinden mail tetikler
        SafeLog(s => s.LogCriticalAsync("UI", "Yakalanmamış AppDomain istisnası", ex, source: "AppDomain"));
        Log.CloseAndFlush();

        if (e.IsTerminating && ShouldNotify(ex))
        {
            try { Dispatcher.Invoke(ShowUnexpectedError); } catch { }
        }
    }

    /// <summary>
    /// UI iş parçacığındaki yakalanmamış istisnalar.
    ///
    /// SIRA ÖNEMLİ: önce KAYIT, sonra bildirim. Bildirim aşaması kendi başına
    /// patlayabilir (diyalog da bir UI işidir); kayıt önce yapılmazsa hatanın
    /// izi tamamen kaybolur.
    ///
    /// Bu akış bir kez gerçek hasara yol açtı: ShellWindow yüklenirken çıkan
    /// bir XAML hatası burada yutuluyordu (Handled=true), kabuk yeniden
    /// ölçülüyor, aynı hata yeniden çıkıyor, her seferinde yeni bir MessageBox
    /// açılıyordu — kullanıcı üst üste hata kutusu görüyor, süreç yığını
    /// tüketip StackOverflow ile ölüyordu. Asıl hata da bu gürültünün altında
    /// kalıyordu.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Yeniden giriş koruması: bildirim aşamasında çıkan istisna buraya
        // geri gelirse döngü kurulur. O durumda yalnızca kaydet ve çık.
        if (_handlingFailure)
        {
            WriteCrashFile(e.Exception, "Hata işlenirken ikinci istisna");
            e.Handled = true;
            return;
        }

        _handlingFailure = true;
        try
        {
            WriteCrashFile(e.Exception, "Yakalanmamış UI (Dispatcher) istisnası");
            Log.Error(e.Exception, "Yakalanmamış UI (Dispatcher) istisnası");
            SafeLog(s => s.LogCriticalAsync("UI", "Yakalanmamış Dispatcher istisnası", e.Exception, source: "UI (Dispatcher)"));

            // Aynı hata tekrar tekrar üretiliyorsa (yeniden çizim/ölçüm
            // döngüsü) kullanıcıya BİR KEZ söylenir; kayıt her seferinde
            // tutulmaya devam eder.
            if (ShouldNotify(e.Exception))
                ShowUnexpectedError();
        }
        finally
        {
            _handlingFailure = false;
        }

        e.Handled = true; // uygulamanın kapanmasını engelle
    }

    // ── Hata bildirimi: tek hata → tek bildirim ──────────────────────────────

    [ThreadStatic]
    private static bool _handlingFailure;

    private static readonly HashSet<string> NotifiedFailures = new(StringComparer.Ordinal);

    /// <summary>
    /// Bu hata için kullanıcıya daha önce bildirim yapıldı mı.
    ///
    /// İmza tür + mesaj + ilk yığın satırından üretilir: aynı kaynaktan gelen
    /// tekrarlar tek sayılır, FARKLI bir hata yine bildirilir. Aksi hâlde ilk
    /// hatadan sonra oluşan gerçek sorunlar sessizce yutulurdu.
    /// </summary>
    private static bool ShouldNotify(Exception? exception)
    {
        var signature = Signature(exception);

        lock (NotifiedFailures)
        {
            // Emniyet supabı: hata fırtınasında bildirim sayısı sınırlı kalsın
            if (NotifiedFailures.Count >= 5) return false;

            return NotifiedFailures.Add(signature);
        }
    }

    private static string Signature(Exception? exception)
    {
        if (exception is null) return "null";

        var firstFrame = exception.StackTrace?
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?
            .Trim() ?? string.Empty;

        return $"{exception.GetType().FullName}|{exception.Message}|{firstFrame}";
    }

    /// <summary>
    /// SON ÇARE kayıt: Serilog ya da veritabanı erişilemese bile hatanın izi
    /// kalsın diye doğrudan dosyaya yazar. İç içe istisnalar dahil edilir —
    /// asıl neden çoğunlukla InnerException'dadır (bu olayda XAML ayrıştırma
    /// hatasının altındaki tip dönüşüm hatasıydı).
    /// </summary>
    private static void WriteCrashFile(Exception? exception, string context)
    {
        try
        {
            var directory = string.IsNullOrWhiteSpace(LogDirectory)
                ? Path.Combine(Path.GetTempPath(), "YonetimFinansal")
                : LogDirectory;

            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, $"crash-{DateTime.Now:yyyyMMdd}.log");

            File.AppendAllText(path,
                $"""

                ===== {DateTime.Now:yyyy-MM-dd HH:mm:ss} — {context} =====
                {exception}

                """);
        }
        catch
        {
            // Dosyaya da yazılamıyorsa yapılabilecek bir şey yok; buradan
            // fırlayan bir istisna hata işlemeyi tamamen çökertirdi.
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Warning(e.Exception, "Gözlemlenmeyen Task istisnası");
        SafeLog(s => s.LogWarningAsync("UI", "Gözlemlenmeyen Task istisnası", source: "Background Task"));
        e.SetObserved();
    }

    /// <summary>
    /// ISystemLogService'e fire-and-forget güvenli erişim.
    /// Services henüz hazır değilse (başlatma öncesi hata) sessizce geçer.
    /// </summary>
    private static void SafeLog(Func<ISystemLogService, Task> action)
    {
        try
        {
            var svc = Services?.GetService<ISystemLogService>();
            if (svc is not null)
                _ = action(svc);
        }
        catch { /* log servisi erişilemiyor — uygulama akışını engelleme */ }
    }

    // ── Config & Logging ─────────────────────────────────────────────────────

    /// <summary>
    /// Ortama göre katmanlı IConfiguration oluşturur:
    ///   appsettings.json  →  appsettings.{Environment}.json  →  ortam değişkenleri
    /// Ortam değişkenleri en son eklenir → en yüksek önceliğe sahiptir.
    /// Ayrıca env var YONETIM_DB_CONNECTION, bağlantı dizesi için InitializeAsync içinde
    /// her zaman öncelikli olarak okunur.
    /// </summary>
    private static IConfiguration BuildConfiguration()
    {
        var basePath = AppContext.BaseDirectory;

        // 1) Ortamı belirle. base appsettings.json'daki AppEnvironment, ClickOnce istemcilerinde
        //    ortam değişkeni taşınmadığı için yedek görevi görür (publish bu değeri Production yapar).
        var baseConfig = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var environment = ResolveEnvironmentName(baseConfig["AppEnvironment"]);

        // 2) Katmanlı yükleme: ortama özel dosya varsa base değerleri geçersiz kılar,
        //    ortam değişkenleri de en üstte kalır.
        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }

    /// <summary>
    /// Aktif ortam adını çözer. Öncelik sırası:
    ///   YONETIM_ENVIRONMENT > DOTNET_ENVIRONMENT > ASPNETCORE_ENVIRONMENT > config AppEnvironment > "Development"
    /// Varsayılan "Development"; böylece geliştirici hiçbir şey ayarlamazsa yanlışlıkla canlı DB'ye bağlanmaz.
    /// </summary>
    private static string ResolveEnvironmentName(string? configFallback)
    {
        return FirstNonEmpty(Environment.GetEnvironmentVariable("YONETIM_ENVIRONMENT"))
            ?? FirstNonEmpty(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"))
            ?? FirstNonEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
            ?? FirstNonEmpty(configFallback)
            ?? "Development";
    }

    private static string? FirstNonEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string ResolveBackupDirectory(IConfiguration config)
    {
        var dir = config["BackupDirectory"] ?? "Backups";
        return Path.IsPathRooted(dir) ? dir : Path.Combine(AppContext.BaseDirectory, dir);
    }

    private static SmtpNotificationOptions BuildSmtpNotificationOptions(IConfiguration config)
    {
        // GÜVENLİK: SMTP şifresi hiçbir appsettings dosyasına yazılmaz.
        // Yalnızca YONETIM_SMTP_PASSWORD ortam değişkeninden okunur; config'teki
        // placeholder değeri bilinçli olarak kullanılmaz.
        var password = Environment.GetEnvironmentVariable("YONETIM_SMTP_PASSWORD") ?? "";
        var username = Environment.GetEnvironmentVariable("YONETIM_SMTP_USERNAME")
                       ?? config["ErrorNotifications:Smtp:Username"] ?? "";

        var enabled = "true".Equals(config["ErrorNotifications:Enabled"], StringComparison.OrdinalIgnoreCase);

        // Env var yoksa uygulama ÇÖKMEZ; yalnızca açıklayıcı uyarı loglanır.
        // SmtpErrorNotificationService şifre boş olduğunda gönderimi kendisi devre dışı bırakır.
        if (enabled && string.IsNullOrWhiteSpace(password))
        {
            Log.Warning(
                "YONETIM_SMTP_PASSWORD ortam değişkeni ayarlanmamış — hata e-posta bildirimleri gönderilemeyecek. " +
                "Ayarlamak için (yönetici PowerShell): " +
                "[System.Environment]::SetEnvironmentVariable('YONETIM_SMTP_PASSWORD','<sifre>','Machine')");
        }

        return new SmtpNotificationOptions
        {
            Enabled       = enabled,
            Provider      = config["ErrorNotifications:Provider"]      ?? "Smtp",
            MinimumLevel  = config["ErrorNotifications:MinimumLevel"]  ?? "Error",
            To            = config["ErrorNotifications:To"]            ?? "",
            From          = config["ErrorNotifications:From"]          ?? "",
            SmtpHost      = config["ErrorNotifications:Smtp:Host"]     ?? "",
            SmtpPort      = int.TryParse(config["ErrorNotifications:Smtp:Port"], out var p) ? p : 587,
            SmtpEnableSsl = !"false".Equals(config["ErrorNotifications:Smtp:EnableSsl"], StringComparison.OrdinalIgnoreCase),
            SmtpUsername  = username,
            SmtpPassword  = password
        };
    }

    private static string ResolveLogDirectory(IConfiguration config)
    {
        var logDir = config["Logging:LogDirectory"] ?? "logs";
        // Göreli yol → AppContext.BaseDirectory altında oluşturulur; mutlak yol olduğu gibi kullanılır
        return Path.IsPathRooted(logDir)
            ? logDir
            : Path.Combine(AppContext.BaseDirectory, logDir);
    }

    private static Serilog.Core.Logger CreateLogger(IConfiguration config, string logDirectory)
    {
        var appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        var logPath    = Path.Combine(logDirectory, "app-.log");

        return new LoggerConfiguration()
            .MinimumLevel.Is(ParseMinimumLevel(config["Logging:MinimumLevel"]))
            // NOT: EF Core SQL komut logu bastırma artık EF-native ConfigureWarnings ile yapılır
            // (ServiceRegistration.AddInfrastructure — "Logging:EfCommandLevel"). Serilog SourceContext
            // override'ı köprüde her zaman güvenilir eşleşmediğinden kaldırıldı.
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("AppVersion", appVersion)
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{MachineName}] [v{AppVersion}] {Message:lj}{NewLine}{Exception}",
                // Günlük dosya 20 MB'ı aşarsa yeni dosyaya döner; tek bir devasa dosya oluşmaz (RAM/açılış güvenliği).
                fileSizeLimitBytes: 20 * 1024 * 1024,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 30,
                encoding: System.Text.Encoding.UTF8)
            .CreateLogger();
    }

    // Ayarda değer yoksa/tanınmazsa fallback döner (Serilog genel min. seviyesi).
    private static LogEventLevel ParseMinimumLevel(string? level, LogEventLevel fallback = LogEventLevel.Information)
        => level?.ToLowerInvariant() switch
        {
            "verbose"     => LogEventLevel.Verbose,
            "debug"       => LogEventLevel.Debug,
            "information" => LogEventLevel.Information,
            "warning"     => LogEventLevel.Warning,
            "error"       => LogEventLevel.Error,
            "fatal"       => LogEventLevel.Fatal,
            _             => fallback
        };

    // EF Core "CommandExecuted" olayının emit seviyesi. Varsayılan Debug = gizli (global min Information).
    // "Information" yapılırsa SQL komutları loglanır (geçici hata ayıklama için).
    private static Microsoft.Extensions.Logging.LogLevel ParseEfCommandLevel(string? level)
        => level?.ToLowerInvariant() switch
        {
            "trace"       => Microsoft.Extensions.Logging.LogLevel.Trace,
            "debug"       => Microsoft.Extensions.Logging.LogLevel.Debug,
            "information" => Microsoft.Extensions.Logging.LogLevel.Information,
            "warning"     => Microsoft.Extensions.Logging.LogLevel.Warning,
            _             => Microsoft.Extensions.Logging.LogLevel.Debug
        };

    private static void ShowConnectionError()
    {
        MessageBox.Show(
            "Veritabanı bağlantısı kurulamadı.\nLütfen ağ bağlantınızı veya sunucu erişimini kontrol edin.",
            "Bağlantı Hatası",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static void ShowUnexpectedError()
    {
        try
        {
            MessageBox.Show(
                "Beklenmeyen bir hata oluştu. Lütfen sistem yöneticisine başvurun.",
                "Beklenmeyen Hata",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch { }
    }

    // ── Session Loop ─────────────────────────────────────────────────────────

    /// <summary>
    /// Her oturum yeni bir IServiceScope alır → ayrı DbContext örneği → oturumlar arası veri karışmaz.
    /// Login iptal / X → Shutdown. Logout → scope dispose + Clear → döngü yeni oturumla devam eder.
    /// </summary>
    private void RunApplicationLoop()
    {
        while (true)
        {
            var scope = Services.CreateScope();
            bool isLogout;

            try
            {
                var loginWindow = new LoginWindow(scope.ServiceProvider);
                Current.MainWindow = loginWindow;

                if (loginWindow.ShowDialog() != true)
                {
                    Shutdown();
                    return;
                }

                var userContext   = scope.ServiceProvider.GetRequiredService<IUserContext>();
                var dialogService = scope.ServiceProvider.GetRequiredService<IDialogService>();
                var startupMode   = ResolveStartupMode(userContext);

                if (startupMode == "none")
                {
                    // Kayıtlı yetki olmayan kullanıcı — login sonrası uyarı + logout
                    dialogService.ShowWarning(
                        "Bu kullanıcı için tanımlı bir başlangıç ekranı bulunamadı.\nLütfen yöneticinizle iletişime geçin.",
                        "Erişim Yok");
                    isLogout = true;
                }
                else
                {
                    // TEK KABUK (Faz D6). Finans ve kargo kullanıcısı aynı
                    // pencereyi görür; ne göreceklerini yetkileri belirler —
                    // navigasyon rayı ve açılış sekmesi ScreenRegistry'deki
                    // yetki kapılarından türer (bkz. ShellWindow).
                    //
                    // startupMode kararı DEĞİŞMEDİ: "none" hâlâ ayrı ele
                    // alınıyor ve yetkisiz kullanıcıya pencere açılmıyor.
                    // "cargo" ile "finance" ayrımı artık pencere seçmiyor,
                    // yalnızca kullanıcının yetkili olduğunu doğruluyor.
                    //
                    // MainWindow ve CargoDashboardWindow SİLİNMEDİ; geri dönüş
                    // ve karşılaştırma için kodda duruyorlar ama başlangıç
                    // akışı artık ikisini de açmıyor.
                    // Sözleşme aynı: ShowDialog + IsLogoutRequested.
                    var shellWindow = new Views.Shell.ShellWindow(scope.ServiceProvider);
                    Current.MainWindow = shellWindow;
                    shellWindow.ShowDialog();
                    isLogout = shellWindow.IsLogoutRequested;
                }
            }
            finally
            {
                // Pencere kapandıktan sonra scope dispose edilir — DbContext, repo bağlantısı serbest bırakılır
                scope.Dispose();
            }

            if (!isLogout)
            {
                // X butonuyla kapandı — uygulamayı kapat
                Shutdown();
                return;
            }

            // Logout: scope dispose sonrası session temizlenir → önceki kullanıcı verisi sonraki oturuma taşınmaz
            Services.GetRequiredService<IUserSession>().Clear();
            // Döngü devam eder: yeni scope, yeni LoginWindow
        }
    }

    /// <summary>
    /// Kullanıcı yetkisine göre giriş sonrası ekranı belirler.
    /// Finans yetkisi varsa "finance", sadece kargo yetkisi varsa "cargo", ikisi de yoksa "none".
    ///
    /// Faz D5'te kabuğa geçiş bu kararı DEĞİŞTİRMEDİ; yalnızca "finance"
    /// dalının açtığı pencere değişti. Karar tabloya bağlı olduğu için
    /// doğrudan test edilebilsin diye internal.
    /// </summary>
    internal static string ResolveStartupMode(IUserContext userContext)
    {
        bool hasFinance = userContext.HasPermission(PermissionType.CanCreateTransaction)
                       || userContext.HasPermission(PermissionType.CanEditTransaction)
                       || userContext.HasPermission(PermissionType.CanDeleteTransaction)
                       || userContext.HasPermission(PermissionType.CanViewReports)
                       || userContext.HasPermission(PermissionType.CanManageUsers)
                       || userContext.HasPermission(PermissionType.CanViewAuditLog)
                       || userContext.HasPermission(PermissionType.CanManageExchangeRates);

        if (hasFinance) return "finance";

        bool hasCargo = userContext.HasPermission(PermissionType.CanViewCargoModule)
                     || userContext.HasPermission(PermissionType.CanViewIncomingCargo)
                     || userContext.HasPermission(PermissionType.CanViewOutgoingCargo)
                     || userContext.HasPermission(PermissionType.CanManageIncomingCargo)
                     || userContext.HasPermission(PermissionType.CanManageOutgoingCargo)
                     || userContext.HasPermission(PermissionType.CanManageCompanyDirectory)
                     || userContext.HasPermission(PermissionType.CanManageCargoCompanies);

        return hasCargo ? "cargo" : "none";
    }
}
