using System.Globalization;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Import;

/// <summary>Finans içe aktarma satırı — durum mantığı ImportRowBase'ten gelir.</summary>
public sealed class CashImportRowDto : ImportRowBase
{
    public DateTime TransactionDate { get; set; }
    public TransactionType TransactionType { get; set; }
    public CurrencyType CurrencyType { get; set; } = CurrencyType.TRY;
    public decimal Amount { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// Mükerrer anahtarı: tarih + tür + para birimi + tutar + normalize açıklama.
    /// Finans işlemlerinde doğal anahtar yoktur — bu bileşim "olası mükerrer"dir,
    /// kullanıcı bilinçli dahil edebilir (aynı gün aynı tutarlı iki meşru işlem olabilir).
    /// </summary>
    public string DuplicateKey =>
        $"{TransactionDate:yyyyMMdd}|{(int)TransactionType}|{(int)CurrencyType}|" +
        $"{Amount.ToString(CultureInfo.InvariantCulture)}|{CompanyNameResolver.Normalize(Description)}";
}

public sealed class CashImportAnalysisResult
{
    public required string SourceName { get; init; }
    public required IReadOnlyList<CashImportRowDto> Rows { get; init; }
    public required int SkippedEmptyRows { get; init; }
    public required IReadOnlyList<string> IgnoredColumns { get; init; }

    public int ValidCount     => Rows.Count(r => r.Status == CargoImportRowStatus.Valid);
    public int WarningCount   => Rows.Count(r => r.Status == CargoImportRowStatus.Warning);
    public int ErrorCount     => Rows.Count(r => r.Status == CargoImportRowStatus.Error);
    public int DuplicateCount => Rows.Count(r => r.Status == CargoImportRowStatus.Duplicate);
}

public class AnalyzeCashImportRequest
{
    public required string FilePath { get; init; }
    public IProgress<ImportProgress>? Progress { get; init; }
}

public class ImportCashTransactionsRequest
{
    public required string SourceName { get; init; }
    public required IReadOnlyList<CashImportRowDto> Rows { get; init; }
    public required Guid CreatedByUserId { get; init; }

    public required int AnalysisTotalRows { get; init; }
    public required int AnalysisValidCount { get; init; }
    public required int AnalysisWarningCount { get; init; }
    public required int AnalysisErrorCount { get; init; }
    public required int AnalysisDuplicateCount { get; init; }

    public IProgress<ImportProgress>? Progress { get; init; }
}

public sealed class CashImportResult
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

    /// <summary>Özet ekranı için tür kırılımı.</summary>
    public required int GirisCount { get; init; }
    public required int CikisCount { get; init; }

    public required Guid ImportedByUserId { get; init; }
    public required string ImportedByUserName { get; init; }
}
