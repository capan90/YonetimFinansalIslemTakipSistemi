using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.IO;
using System.Reflection;
using System.Windows;
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
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Label.GenerateCargoLabel;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification.GenerateCargoNotification;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification.MarkCargoNotificationPrepared;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoDashboard;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoReport;
using YonetimFinansalIslemTakipSistemi.Application.Features.Settings.MailSettings;
using YonetimFinansalIslemTakipSistemi.Application.Services;
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

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

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

        var appEnvironment = config["AppEnvironment"] ?? "Development";
        var isDevelopment  = !appEnvironment.Equals("Production", StringComparison.OrdinalIgnoreCase);

        Log.Debug("Ortam: {AppEnvironment}", appEnvironment);

        var services = new ServiceCollection();
        // IConfiguration'ı DI'a al — AesSecretProtector ve diğer servisler için
        services.AddSingleton<IConfiguration>(config);
        services.AddInfrastructure(connectionString);

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

        // Kargo Katip handler'lar
        services.AddScoped<CreateCompanyDirectoryHandler>();
        services.AddScoped<UpdateCompanyDirectoryHandler>();
        services.AddScoped<DeleteCompanyDirectoryHandler>();
        services.AddScoped<GetCompanyDirectoryListHandler>();
        services.AddScoped<GetCompanyAttentionContactsHandler>();
        services.AddScoped<EnsureCompanyAttentionContactHandler>();
        services.AddScoped<CreateCargoCompanyHandler>();
        services.AddScoped<UpdateCargoCompanyHandler>();
        services.AddScoped<DeleteCargoCompanyHandler>();
        services.AddScoped<GetCargoCompanyListHandler>();
        services.AddScoped<CreateCargoShipmentHandler>();
        services.AddScoped<UpdateCargoShipmentHandler>();
        services.AddScoped<DeleteCargoShipmentHandler>();
        services.AddScoped<QuickUpdateCargoStatusHandler>();
        services.AddScoped<GetCargoShipmentListHandler>();
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

        // Kargo Katip ViewModels
        services.AddTransient<CompanyDirectoryListViewModel>();
        services.AddTransient<CompanyDirectoryEditViewModel>();
        services.AddTransient<CargoCompanyListViewModel>();
        services.AddTransient<CargoCompanyEditViewModel>();
        services.AddTransient<CargoShipmentEditViewModel>();

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

        Log.Information("Uygulama başlatıldı, oturum döngüsü başlıyor");
        SafeLog(s => s.LogInfoAsync("Startup", "Uygulama başlatıldı", source: "App.InitializeAsync"));
        RunApplicationLoop();
    }

    // ── Global Exception Handlers ────────────────────────────────────────────

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        Log.Fatal(ex, "Yakalanmamış uygulama istisnası (AppDomain). Kapanıyor: {IsTerminating}", e.IsTerminating);
        // SystemLogService kritik log yazar ve içinden mail tetikler
        SafeLog(s => s.LogCriticalAsync("UI", "Yakalanmamış AppDomain istisnası", ex, source: "AppDomain"));
        Log.CloseAndFlush();

        if (e.IsTerminating)
        {
            try { Dispatcher.Invoke(ShowUnexpectedError); } catch { }
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Yakalanmamış UI (Dispatcher) istisnası");
        SafeLog(s => s.LogCriticalAsync("UI", "Yakalanmamış Dispatcher istisnası", e.Exception, source: "UI (Dispatcher)"));
        ShowUnexpectedError();
        e.Handled = true; // uygulamanın kapanmasını engelle
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
    /// appsettings.json'dan IConfiguration oluşturur.
    /// Env var YONETIM_DB_CONNECTION bağlantı dizesi için her zaman önceliklidir.
    /// </summary>
    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();
    }

    private static string ResolveBackupDirectory(IConfiguration config)
    {
        var dir = config["BackupDirectory"] ?? "Backups";
        return Path.IsPathRooted(dir) ? dir : Path.Combine(AppContext.BaseDirectory, dir);
    }

    private static SmtpNotificationOptions BuildSmtpNotificationOptions(IConfiguration config)
    {
        // Şifre/kullanıcı env var'dan okunur — appsettings.Production.json'a şifre yazılmaz
        var password = Environment.GetEnvironmentVariable("YONETIM_SMTP_PASSWORD")
                       ?? config["ErrorNotifications:Smtp:Password"] ?? "";
        var username = Environment.GetEnvironmentVariable("YONETIM_SMTP_USERNAME")
                       ?? config["ErrorNotifications:Smtp:Username"] ?? "";

        return new SmtpNotificationOptions
        {
            Enabled       = "true".Equals(config["ErrorNotifications:Enabled"],     StringComparison.OrdinalIgnoreCase),
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
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("AppVersion", appVersion)
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{MachineName}] [v{AppVersion}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 30,
                encoding: System.Text.Encoding.UTF8)
            .CreateLogger();
    }

    private static LogEventLevel ParseMinimumLevel(string? level) => level?.ToLowerInvariant() switch
    {
        "verbose" => LogEventLevel.Verbose,
        "debug"   => LogEventLevel.Debug,
        "warning" => LogEventLevel.Warning,
        "error"   => LogEventLevel.Error,
        "fatal"   => LogEventLevel.Fatal,
        _         => LogEventLevel.Information
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
                else if (startupMode == "cargo")
                {
                    // Sadece kargo yetkisi var — finans ekranı gösterilmez
                    var cargoWindow = new CargoDashboardWindow(scope.ServiceProvider);
                    Current.MainWindow = cargoWindow;
                    cargoWindow.ShowDialog();
                    isLogout = true; // kargo penceresi kapandığında login ekranına dön
                }
                else
                {
                    // Finans yetkisi var — mevcut davranış
                    var mainWindow = new MainWindow(scope.ServiceProvider);
                    Current.MainWindow = mainWindow;
                    mainWindow.ShowDialog();
                    isLogout = mainWindow.IsLogoutRequested;
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
    /// </summary>
    private static string ResolveStartupMode(IUserContext userContext)
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
