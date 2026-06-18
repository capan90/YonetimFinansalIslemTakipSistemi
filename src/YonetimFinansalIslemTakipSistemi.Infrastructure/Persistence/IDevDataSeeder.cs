namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence;

/// <summary>
/// [DEV-ONLY] Geliştirme ortamında ilk çalıştırmada zorunlu seed verilerini oluşturur.
/// Üretim ortamında bu interface ve implementasyonu kaldırılacak;
/// kullanıcılar yönetim ekranından oluşturulacak.
/// </summary>
public interface IDevDataSeeder
{
    /// <summary>
    /// Yalnızca users tablosu boşsa çalışır; mevcut veriyi değiştirmez (idempotent).
    /// </summary>
    Task SeedAsync();
}
