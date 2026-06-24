using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

public class DatabaseConnectionTestService : IDatabaseConnectionTestService
{
    private readonly AppDbContext _dbContext;

    public DatabaseConnectionTestService(AppDbContext dbContext)
        => _dbContext = dbContext;

    public async Task<bool> CanConnectAsync()
    {
        try
        {
            return await _dbContext.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }
}
