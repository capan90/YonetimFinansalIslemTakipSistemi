using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// İçe aktarma kaynağını format bağımsız ImportDocument'a çevirir.
/// Mevcut implementasyon Excel'dir (Infrastructure); ileride CSV/XML
/// aynı sözleşmenin yeni implementasyonları olarak eklenir.
/// Dosya seviyesi sorunlarda (uzantı, boyut, bozuk içerik, satır limiti)
/// kullanıcıya gösterilebilir mesajla ImportFileException fırlatır.
/// </summary>
public interface ICargoImportFileReader
{
    Task<ImportDocument> ReadAsync(string filePath);
}

/// <summary>Kullanıcının dolduracağı boş içe aktarma şablonunu üretir (mevcut format: xlsx).</summary>
public interface ICargoImportTemplateService
{
    void CreateTemplate(string filePath);
}
