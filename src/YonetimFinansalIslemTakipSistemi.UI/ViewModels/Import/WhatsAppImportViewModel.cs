using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Import;

/// <summary>WhatsApp rehberi içe aktarma sihirbazının durumu.</summary>
public class WhatsAppImportViewModel : ImportWizardViewModelBase<WhatsAppImportRowItem>
{
    private readonly AnalyzeWhatsAppImportHandler _analyzeHandler;
    private readonly ImportWhatsAppContactsHandler _importHandler;
    private readonly IUserContext _userContext;

    private WhatsAppImportAnalysisResult? _analysis;

    public WhatsAppImportViewModel(
        AnalyzeWhatsAppImportHandler analyzeHandler,
        ImportWhatsAppContactsHandler importHandler,
        IUserContext userContext)
    {
        _analyzeHandler = analyzeHandler;
        _importHandler  = importHandler;
        _userContext    = userContext;
    }

    public WhatsAppImportResult? LastResult { get; private set; }

    public async Task<string?> AnalyzeAsync(string filePath)
    {
        IsBusy = true;
        ProgressIndeterminate = true;
        SetProgressText("Dosya okunuyor…");
        try
        {
            var result = await _analyzeHandler.HandleAsync(new AnalyzeWhatsAppImportRequest
            {
                FilePath = filePath,
                Progress = new Progress<Application.Features.CargoShipment.Import.ImportProgress>(ReportProgress)
            });

            if (!result.Success)
                return result.ErrorMessage ?? "Dosya analiz edilemedi.";

            _analysis = result.Data!;
            LoadRows(
                _analysis.Rows.Select(dto => new WhatsAppImportRowItem(dto, OnRowInclusionChanged)).ToList(),
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

    public async Task<OperationResult<WhatsAppImportResult>> ImportAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _importHandler.HandleAsync(new ImportWhatsAppContactsRequest
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

public class WhatsAppImportRowItem : ImportRowItemBase
{
    public WhatsAppImportRowDto Dto { get; }

    public WhatsAppImportRowItem(WhatsAppImportRowDto dto, Action inclusionChanged)
        : base(dto, inclusionChanged) => Dto = dto;

    public string NameDisplay        => Dto.FullName ?? "—";
    public string PhoneDisplay       => Dto.NormalizedPhone ?? "—";
    public string CompanyDisplay     => Dto.Company ?? "—";
    public string DescriptionDisplay => Dto.Description ?? "—";
}
