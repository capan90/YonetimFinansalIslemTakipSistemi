using YonetimFinansalIslemTakipSistemi.Application.Common;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

public interface IHealthCheckService
{
    Task<AppHealthInfo> GetHealthAsync();
}
