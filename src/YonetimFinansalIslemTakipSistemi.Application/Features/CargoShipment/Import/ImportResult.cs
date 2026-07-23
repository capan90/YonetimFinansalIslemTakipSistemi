using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

/// <summary>
/// Tamamlanan bir içe aktarma işleminin özeti. Sonuç (Summary) adımının veri
/// kaynağıdır ve audit özet kaydına yazılır. İleride Import History ekranı
/// bu modelin kalıcılaştırılmış hali üzerine kurulacaktır — alan eklerken
/// geriye dönük uyumluluğu koruyun.
/// </summary>
public sealed class ImportResult
{
    public required Guid ImportId { get; init; }
    public required string SourceName { get; init; }
    public required CargoShipmentDirection Direction { get; init; }

    public required DateTime StartedAtUtc { get; init; }
    public required DateTime CompletedAtUtc { get; init; }

    /// <summary>Analizde okunan toplam veri satırı (boş satırlar hariç).</summary>
    public required int TotalRows { get; init; }
    public required int ValidCount { get; init; }
    public required int WarningCount { get; init; }
    public required int ErrorCount { get; init; }
    public required int DuplicateCount { get; init; }

    /// <summary>Kullanıcının onaylayıp gönderdiği satır sayısı.</summary>
    public required int RequestedCount { get; init; }

    /// <summary>Gerçekten oluşturulan kayıt sayısı (transaction başarılıysa = RequestedCount).</summary>
    public required int ImportedCount { get; init; }

    /// <summary>Üretilen numara aralığı (örn. GDN00045 – GDN00081).</summary>
    public string? FirstShipmentNumber { get; init; }
    public string? LastShipmentNumber { get; init; }

    public required Guid ImportedByUserId { get; init; }
    public required string ImportedByUserName { get; init; }
}
