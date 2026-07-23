using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

/// <summary>Analiz aşamasının çıktısı — önizleme ekranının tek veri kaynağı.</summary>
public sealed class CargoImportAnalysisResult
{
    public required string SourceName { get; init; }
    public required CargoShipmentDirection Direction { get; init; }
    public required IReadOnlyList<CargoImportRowDto> Rows { get; init; }

    /// <summary>Sessizce atlanan boş satır sayısı.</summary>
    public required int SkippedEmptyRows { get; init; }

    /// <summary>Şemada karşılığı olmayan (yok sayılan) kolon başlıkları.</summary>
    public required IReadOnlyList<string> IgnoredColumns { get; init; }

    public int ValidCount     => Rows.Count(r => r.Status == CargoImportRowStatus.Valid);
    public int WarningCount   => Rows.Count(r => r.Status == CargoImportRowStatus.Warning);
    public int ErrorCount     => Rows.Count(r => r.Status == CargoImportRowStatus.Error);
    public int DuplicateCount => Rows.Count(r => r.Status == CargoImportRowStatus.Duplicate);
}

/// <summary>Uzun işlemlerde UI'a bildirilen ilerleme (işlenen/toplam satır).</summary>
public sealed record ImportProgress(string Phase, int Processed, int Total);
