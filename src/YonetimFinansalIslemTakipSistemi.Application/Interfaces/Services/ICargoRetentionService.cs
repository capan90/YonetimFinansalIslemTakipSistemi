namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

public interface ICargoRetentionService
{
    /// <summary>
    /// 6 aydan eski kargo kayıtlarını DB'den kalıcı olarak siler.
    /// Her gün en fazla bir kez çalışır; son çalışma zamanı ApplicationSettings'te tutulur.
    /// </summary>
    Task RunAsync();
}
