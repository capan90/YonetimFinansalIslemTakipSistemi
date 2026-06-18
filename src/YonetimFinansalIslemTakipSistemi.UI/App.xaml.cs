using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.CreateCashTransaction;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCashTransactions;
using YonetimFinansalIslemTakipSistemi.Infrastructure;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;

namespace YonetimFinansalIslemTakipSistemi.UI;

public partial class App : System.Windows.Application
{
    /// <summary>
    /// Uygulama genelinde kullanılabilecek service provider.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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
        services.AddTransient<CashTransactionListViewModel>();

        Services = services.BuildServiceProvider();

        // Scope, MainWindow'un ömrüyle eşleştirilir
        var scope  = Services.CreateScope();
        var window = new MainWindow(scope.ServiceProvider);
        window.Closed += (_, _) => scope.Dispose();
        window.Show();
    }
}
