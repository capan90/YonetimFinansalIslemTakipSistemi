using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.Import;

/// <summary>WhatsApp rehberi içe aktarma satırı — Phone alanı normalize edilmiş (+90 5XX) numaradır.</summary>
public sealed class WhatsAppImportRowDto : ImportRowBase
{
    public string? FullName { get; set; }

    /// <summary>PhoneNumberNormalizer.NormalizeTr çıktısı — analiz başarılıysa doludur.</summary>
    public string? NormalizedPhone { get; set; }

    public string? Company { get; set; }
    public string? Description { get; set; }

    /// <summary>Numara soft-delete edilmiş bir kayıtta bulunduysa import geri yükler.</summary>
    public Guid? ResurrectContactId { get; set; }
}

public sealed class WhatsAppImportAnalysisResult
{
    public required string SourceName { get; init; }
    public required IReadOnlyList<WhatsAppImportRowDto> Rows { get; init; }
    public required int SkippedEmptyRows { get; init; }
    public required IReadOnlyList<string> IgnoredColumns { get; init; }

    public int ValidCount     => Rows.Count(r => r.Status == CargoImportRowStatus.Valid);
    public int WarningCount   => Rows.Count(r => r.Status == CargoImportRowStatus.Warning);
    public int ErrorCount     => Rows.Count(r => r.Status == CargoImportRowStatus.Error);
    public int DuplicateCount => Rows.Count(r => r.Status == CargoImportRowStatus.Duplicate);
}

public class AnalyzeWhatsAppImportRequest
{
    public required string FilePath { get; init; }
    public IProgress<ImportProgress>? Progress { get; init; }
}

public class ImportWhatsAppContactsRequest
{
    public required string SourceName { get; init; }
    public required IReadOnlyList<WhatsAppImportRowDto> Rows { get; init; }
    public required Guid CreatedByUserId { get; init; }

    public required int AnalysisTotalRows { get; init; }
    public required int AnalysisValidCount { get; init; }
    public required int AnalysisWarningCount { get; init; }
    public required int AnalysisErrorCount { get; init; }
    public required int AnalysisDuplicateCount { get; init; }

    public IProgress<ImportProgress>? Progress { get; init; }
}

public sealed class WhatsAppImportResult
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

    /// <summary>Soft-delete durumundan geri yüklenen kayıt sayısı (ImportedCount'a dahildir).</summary>
    public required int ResurrectedCount { get; init; }

    public required Guid ImportedByUserId { get; init; }
    public required string ImportedByUserName { get; init; }
}
