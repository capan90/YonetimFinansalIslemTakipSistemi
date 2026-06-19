using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.AuditLogs.Queries.GetAuditLogs;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.CreateCashTransaction;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.DeleteCashTransaction;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.UpdateCashTransaction;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCashTransactions;
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
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.ExchangeRates;

namespace YonetimFinansalIslemTakipSistemi.UI;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var connectionString = Environment.GetEnvironmentVariable("YONETIM_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=yonetim_db;Username=postgres;Password=postgres123";

        var services = new ServiceCollection();
        services.AddInfrastructure(connectionString);

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

        Services = services.BuildServiceProvider();

        // [DEV-ONLY] Seed bir kez, kendi kısa ömürlü scope'uyla
        using (var seedScope = Services.CreateScope())
            await seedScope.ServiceProvider.GetRequiredService<IDevDataSeeder>().SeedAsync();

        RunApplicationLoop();
    }

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
