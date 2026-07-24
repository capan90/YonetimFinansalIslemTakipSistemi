using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Import;

/// <summary>Firma rehberi içe aktarma satırı — durum mantığı ImportRowBase'ten gelir.</summary>
public sealed class DirectoryImportRowDto : ImportRowBase
{
    public string? CompanyName { get; set; }
    public string? ContactPerson { get; set; }
    public string? AttentionTo { get; set; }
    public string? AddressLine { get; set; }
    public string? District { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
}

public sealed class DirectoryImportAnalysisResult
{
    public required string SourceName { get; init; }
    public required IReadOnlyList<DirectoryImportRowDto> Rows { get; init; }
    public required int SkippedEmptyRows { get; init; }
    public required IReadOnlyList<string> IgnoredColumns { get; init; }

    public int ValidCount     => Rows.Count(r => r.Status == CargoImportRowStatus.Valid);
    public int WarningCount   => Rows.Count(r => r.Status == CargoImportRowStatus.Warning);
    public int ErrorCount     => Rows.Count(r => r.Status == CargoImportRowStatus.Error);
    public int DuplicateCount => Rows.Count(r => r.Status == CargoImportRowStatus.Duplicate);
}

public class AnalyzeDirectoryImportRequest
{
    public required string FilePath { get; init; }
    public IProgress<ImportProgress>? Progress { get; init; }
}

public class ImportDirectoryEntriesRequest
{
    public required string SourceName { get; init; }
    public required IReadOnlyList<DirectoryImportRowDto> Rows { get; init; }
    public required Guid CreatedByUserId { get; init; }

    public required int AnalysisTotalRows { get; init; }
    public required int AnalysisValidCount { get; init; }
    public required int AnalysisWarningCount { get; init; }
    public required int AnalysisErrorCount { get; init; }
    public required int AnalysisDuplicateCount { get; init; }

    public IProgress<ImportProgress>? Progress { get; init; }
}

/// <summary>Rehber içe aktarma sonucu — ImportResult ile aynı sözleşme, yön alanı yok.</summary>
public sealed class DirectoryImportResult
{
    public required Guid ImportId { get; init; }
    public required string SourceName { get; init; }
    public required DateTime StartedAtUtc { get; init; }
    public required DateTime CompletedAtUtc { get; init; }
    public required int TotalRows { get; init; }
    public required int ValidCount { get; init; }
    public required int WarningCount { get; init; }
    public required int ErrorCount { get; init; }
    public required int DuplicateCount { get; init; }
    public required int RequestedCount { get; init; }
    public required int ImportedCount { get; init; }
    public required Guid ImportedByUserId { get; init; }
    public required string ImportedByUserName { get; init; }
}
