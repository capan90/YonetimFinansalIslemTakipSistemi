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
            Log.CloseAndFlush();
            ShowConnectionError();
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Uygulama kapatılıyor");
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
        services.AddInfrastructure(connectionString);

        // HealthCheckService için ortam bilgileri — UI katmanı bu nesneyi oluşturur
        // çünkü DeploymentSettings ve App.LogDirectory burada erişilebilir.
        services.AddSingleton(new HealthCheckOptions
        {
            AppEnvironment    = appEnvironment,
            LogDirectory      = LogDirectory,
            BackupDirectory   = ResolveBackupDirectory(config),
            UpdatePublishPath = DeploymentSettings.PublishPath,
            ConnectionString  = connectionString
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

        Services = services.BuildServiceProvider();

        // Veritabanı bağlantısını erken test et — hata varsa kullanıcı dostu mesaj göster
        using (var testScope = Services.CreateScope())
        {
            var testService = testScope.ServiceProvider.GetRequiredService<IDatabaseConnectionTestService>();
            if (!await testService.CanConnectAsync())
            {
                Log.Error("Veritabanına bağlanılamadı — startup iptal ediliyor");
                throw new InvalidOperationException("Veritabanına bağlanılamadı.");
            }
        }

        Log.Information("Veritabanı bağlantısı doğrulandı");

        // [DEV-ONLY] Seed yalnızca geliştirme ortamında çalışır
        if (isDevelopment)
        {
            using var seedScope = Services.CreateScope();
            await seedScope.ServiceProvider.GetRequiredService<IDevDataSeeder>().SeedAsync();
        }

        Log.Information("Uygulama başlatıldı, oturum döngüsü başlıyor");
        RunApplicationLoop();
    }

    // ── Global Exception Handlers ────────────────────────────────────────────

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        Log.Fatal(ex, "Yakalanmamış uygulama istisnası (AppDomain). Kapanıyor: {IsTerminating}", e.IsTerminating);
        Log.CloseAndFlush();

        if (e.IsTerminating)
        {
            // Uygulama kapanmak üzere; MessageBox göstermeye çalış
            try { Dispatcher.Invoke(ShowUnexpectedError); } catch { }
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Yakalanmamış UI (Dispatcher) istisnası");
        ShowUnexpectedError();
        e.Handled = true; // uygulamanın kapanmasını engelle
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // Task istisnası: gözlemlenmiş olarak işaretle, log'a yaz, uygulamayı kapatma
        Log.Warning(e.Exception, "Gözlemlenmeyen Task istisnası");
        e.SetObserved();
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

                var mainWindow = new MainWindow(scope.ServiceProvider);
                Current.MainWindow = mainWindow;
                mainWindow.ShowDialog(); // MainWindow kapanana kadar bloke eder

                isLogout = mainWindow.IsLogoutRequested;
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
}
