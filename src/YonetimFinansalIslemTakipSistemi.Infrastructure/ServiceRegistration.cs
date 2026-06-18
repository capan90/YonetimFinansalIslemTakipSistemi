using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Repositories;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure;

/// <summary>
/// Infrastructure servislerini DI container'a kaydeden extension.
/// UI katmanı Npgsql detaylarını bilmez; sadece bu metodu çağırır.
/// </summary>
public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ICashTransactionRepository, CashTransactionRepository>();

        return services;
    }
}
