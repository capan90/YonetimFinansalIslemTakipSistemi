using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

public class ImportCargoShipmentsRequest
{
    public required CargoShipmentDirection Direction { get; init; }
    public required string SourceName { get; init; }

    /// <summary>Kullanıcının önizlemede onayladığı (dahil ettiği) satırlar.</summary>
    public required IReadOnlyList<CargoImportRowDto> Rows { get; init; }

    public required Guid CreatedByUserId { get; init; }

    /// <summary>Sonuç modeline yazılacak analiz özet sayıları.</summary>
    public required int AnalysisTotalRows { get; init; }
    public required int AnalysisValidCount { get; init; }
    public required int AnalysisWarningCount { get; init; }
    public required int AnalysisErrorCount { get; init; }
    public required int AnalysisDuplicateCount { get; init; }

    public IProgress<ImportProgress>? Progress { get; init; }
}
