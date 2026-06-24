namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

public interface IDatabaseConnectionTestService
{
    /// <summary>
    /// Veritabanına bağlanılıp bağlanılamadığını kontrol eder.
    /// Ağ hatası veya kimlik doğrulama başarısızlığında false döner; exception fırlatmaz.
    /// </summary>
    Task<bool> CanConnectAsync();
}
