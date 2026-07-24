using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Import;

/// <summary>Firma rehberi içe aktarma sihirbazının durumu.</summary>
public class DirectoryImportViewModel : ImportWizardViewModelBase<DirectoryImportRowItem>
{
    private readonly AnalyzeDirectoryImportHandler _analyzeHandler;
    private readonly ImportDirectoryEntriesHandler _importHandler;
    private readonly IUserContext _userContext;

    private DirectoryImportAnalysisResult? _analysis;

    public DirectoryImportViewModel(
        AnalyzeDirectoryImportHandler analyzeHandler,
        ImportDirectoryEntriesHandler importHandler,
        IUserContext userContext)
    {
        _analyzeHandler = analyzeHandler;
        _importHandler  = importHandler;
        _userContext    = userContext;
    }

    public DirectoryImportResult? LastResult { get; private set; }

    public async Task<string?> AnalyzeAsync(string filePath)
    {
        IsBusy = true;
        ProgressIndeterminate = true;
        SetProgressText("Dosya okunuyor…");
        try
        {
            var result = await _analyzeHandler.HandleAsync(new AnalyzeDirectoryImportRequest
            {
                FilePath = filePath,
                Progress = new Progress<Application.Features.CargoShipment.Import.ImportProgress>(ReportProgress)
            });

            if (!result.Success)
                return result.ErrorMessage ?? "Dosya analiz edilemedi.";

            _analysis = result.Data!;
            LoadRows(
                _analysis.Rows.Select(dto => new DirectoryImportRowItem(dto, OnRowInclusionChanged)).ToList(),
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

    public async Task<OperationResult<DirectoryImportResult>> ImportAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _importHandler.HandleAsync(new ImportDirectoryEntriesRequest
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

public class DirectoryImportRowItem : ImportRowItemBase
{
    public DirectoryImportRowDto Dto { get; }

    public DirectoryImportRowItem(DirectoryImportRowDto dto, Action inclusionChanged)
        : base(dto, inclusionChanged) => Dto = dto;

    public string CompanyDisplay => Dto.CompanyName ?? "—";
    public string ContactDisplay => Dto.ContactPerson ?? "—";
    public string PhoneDisplay   => Dto.Phone ?? "—";
    public string CityDisplay    => Dto.City ?? "—";
    public string NotesDisplay   => Dto.Notes ?? "—";
}
