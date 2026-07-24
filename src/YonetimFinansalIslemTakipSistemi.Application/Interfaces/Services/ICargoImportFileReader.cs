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

/// <summary>Kullanıcının dolduracağı boş içe aktarma şablonlarını üretir (mevcut format: xlsx).</summary>
public interface ICargoImportTemplateService
{
    /// <summary>Kargo gönderi şablonu.</summary>
    void CreateTemplate(string filePath);

    /// <summary>Firma rehberi şablonu.</summary>
    void CreateDirectoryTemplate(string filePath);

    /// <summary>WhatsApp rehberi şablonu.</summary>
    void CreateWhatsAppTemplate(string filePath);

    /// <summary>Finans işlem şablonu.</summary>
    void CreateCashTemplate(string filePath);
}
