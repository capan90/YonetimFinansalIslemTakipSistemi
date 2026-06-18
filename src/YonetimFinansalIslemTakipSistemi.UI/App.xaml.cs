using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.CreateCashTransaction;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCashTransactions;
using YonetimFinansalIslemTakipSistemi.Infrastructure;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Login;

namespace YonetimFinansalIslemTakipSistemi.UI;

public partial class App : System.Windows.Application
{
    /// <summary>
    /// Uygulama genelinde kullanılabilecek service provider.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Dialog kapandığında app'in otomatik kapanmasını engeller; shutdown bu metotta kontrol edilir.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Bağlantı bilgisi önce ortam değişkeninden okunur; ayarlanmamışsa yerel varsayılan kullanılır.
        var connectionString = Environment.GetEnvironmentVariable("YONETIM_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=yonetim_db;Username=postgres;Password=postgres123";

        var services = new ServiceCollection();
        services.AddInfrastructure(connectionString);

        // Command handler'lar
        services.AddScoped<CreateCashTransactionHandler>();

        // Query handler'lar
        services.AddScoped<GetCashTransactionsHandler>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<CashTransactionListViewModel>();

        Services = services.BuildServiceProvider();

        var scope = Services.CreateScope();

        // [DEV-ONLY] Seed servisini çalıştır; mantık Infrastructure'da kapsüllü
        await scope.ServiceProvider.GetRequiredService<IDevDataSeeder>().SeedAsync();

        // Önce login ekranı; iptal veya başarısız girişte uygulamayı kapat.
        var loginWindow = new LoginWindow(scope.ServiceProvider);
        if (loginWindow.ShowDialog() != true)
        {
            scope.Dispose();
            Shutdown();
            return;
        }

        // Başarılı giriş — ana pencereyi aç; kapanınca uygulama sonlanır.
        var mainWindow = new MainWindow(scope.ServiceProvider);
        mainWindow.Closed += (_, _) => { scope.Dispose(); Shutdown(); };
        mainWindow.Show();
    }
}
