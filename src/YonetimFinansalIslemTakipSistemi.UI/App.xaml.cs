using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.CreateCashTransaction;
using YonetimFinansalIslemTakipSistemi.Infrastructure;

namespace YonetimFinansalIslemTakipSistemi.UI;

public partial class App : System.Windows.Application
{
    /// <summary>
    /// Uygulama genelinde kullanılabilecek service provider.
    /// Pencereler ve bileşenler buradan servis çözümleyebilir.
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
        services.AddScoped<CreateCashTransactionHandler>();

        Services = services.BuildServiceProvider();

        new MainWindow().Show();
    }
}

