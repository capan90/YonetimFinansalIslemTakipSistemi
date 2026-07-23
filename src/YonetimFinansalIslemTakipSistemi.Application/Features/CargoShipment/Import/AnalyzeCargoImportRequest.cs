using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

public class AnalyzeCargoImportRequest
{
    public required string FilePath { get; init; }
    public required CargoShipmentDirection Direction { get; init; }

    /// <summary>Opsiyonel ilerleme bildirimi (işlenen/toplam satır) — UI progress bar'ı besler.</summary>
    public IProgress<ImportProgress>? Progress { get; init; }
}
