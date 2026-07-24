using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Import;

/// <summary>Finans içe aktarma sihirbazının durumu.</summary>
public class CashImportViewModel : ImportWizardViewModelBase<CashImportRowItem>
{
    private readonly AnalyzeCashImportHandler _analyzeHandler;
    private readonly ImportCashTransactionsHandler _importHandler;
    private readonly IUserContext _userContext;

    private CashImportAnalysisResult? _analysis;

    public CashImportViewModel(
        AnalyzeCashImportHandler analyzeHandler,
        ImportCashTransactionsHandler importHandler,
        IUserContext userContext)
    {
        _analyzeHandler = analyzeHandler;
        _importHandler  = importHandler;
        _userContext    = userContext;
    }

    public CashImportResult? LastResult { get; private set; }

    public async Task<string?> AnalyzeAsync(string filePath)
    {
        IsBusy = true;
        ProgressIndeterminate = true;
        SetProgressText("Dosya okunuyor…");
        try
        {
            var result = await _analyzeHandler.HandleAsync(new AnalyzeCashImportRequest
            {
                FilePath = filePath,
                Progress = new Progress<Application.Features.CargoShipment.Import.ImportProgress>(ReportProgress)
            });

            if (!result.Success)
                return result.ErrorMessage ?? "Dosya analiz edilemedi.";

            _analysis = result.Data!;
            LoadRows(
                _analysis.Rows.Select(dto => new CashImportRowItem(dto, OnRowInclusionChanged)).ToList(),
                _analysis.Rows.Count, _analysis.ValidCount, _analysis.WarningCount,
                _analysis.ErrorCount, _analysis.DuplicateCount,
                _analysis.SkippedEmptyRows, _analysis.IgnoredColumns);
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<OperationResult<CashImportResult>> ImportAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _importHandler.HandleAsync(new ImportCashTransactionsRequest
            {
                SourceName             = _analysis!.SourceName,
                Rows                   = IncludedItems.Select(i => i.Dto).ToList(),
                CreatedByUserId        = _userContext.UserId,
                AnalysisTotalRows      = _analysis.Rows.Count,
                AnalysisValidCount     = _analysis.ValidCount,
                AnalysisWarningCount   = _analysis.WarningCount,
                AnalysisErrorCount     = _analysis.ErrorCount,
                AnalysisDuplicateCount = _analysis.DuplicateCount,
                Progress               = new Progress<Application.Features.CargoShipment.Import.ImportProgress>(ReportProgress)
            });

            if (result.Success) LastResult = result.Data;
            return result;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class CashImportRowItem : ImportRowItemBase
{
    public CashImportRowDto Dto { get; }

    public CashImportRowItem(CashImportRowDto dto, Action inclusionChanged)
        : base(dto, inclusionChanged) => Dto = dto;

    public string DateDisplay     => Dto.TransactionDate == default ? "—" : Dto.TransactionDate.ToString("dd.MM.yyyy");
    public string TypeDisplay     => Dto.Amount <= 0 ? "—"
        : Dto.TransactionType == TransactionType.Giris ? "Giriş" : "Çıkış";
    public string CurrencyDisplay => Dto.CurrencyType.ToString() == "TRY" ? "TL" : Dto.CurrencyType.ToString();
    public string AmountDisplay   => Dto.Amount <= 0 ? "—"
        : Dto.Amount.ToString("N2", new System.Globalization.CultureInfo("tr-TR"));
    public string DescriptionDisplay => Dto.Description ?? "—";
}
