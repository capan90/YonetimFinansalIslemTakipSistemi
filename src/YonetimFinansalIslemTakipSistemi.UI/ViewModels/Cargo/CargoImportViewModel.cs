using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Import;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;

/// <summary>
/// Kargo Excel içe aktarma sihirbazının durumu: Dosya Seç → Önizleme →
/// İçe Aktarma → Sonuç.
///
/// İlerleme, durum filtresi, özet ve seçim yönetimi ortak tabandan gelir
/// (<see cref="ImportWizardViewModelBase{TItem}"/>); burada yalnızca KARGOYA
/// ÖZGÜ olan kalır: hangi handler'lar çağrılır, hangi istek kurulur.
///
/// TEKNİK BORÇ KAPANDI (Faz F3): bu sınıf ortak tabandan ÖNCE yazılmıştı ve
/// tabanın tamamının kopyasını taşıyordu. Rehber/WhatsApp/Nakit sihirbazları
/// tabana geçerken bu geride kalmıştı; filtre ya da ilerleme davranışında bir
/// düzeltme üçünde uygulanıp burada unutulabilirdi.
///
/// Dosya diyalogları ve panel görünürlüğü window code-behind'dadır.
/// </summary>
public class CargoImportViewModel(
    AnalyzeCargoImportHandler   analyzeHandler,
    ImportCargoShipmentsHandler importHandler,
    IUserContext                userContext,
    CargoShipmentDirection      direction)
    : ImportWizardViewModelBase<CargoImportRowItem>
{
    public CargoShipmentDirection Direction { get; } = direction;

    public CargoImportAnalysisResult? Analysis { get; private set; }

    public ImportResult? LastResult { get; private set; }

    /// <summary>
    /// Dosyayı analiz eder ve önizlemeyi doldurur.
    /// </summary>
    /// <returns>Hata mesajı; <c>null</c> ise başarı.</returns>
    public async Task<string?> AnalyzeAsync(string filePath)
    {
        IsBusy = true;
        ProgressIndeterminate = true;
        SetProgressText("Dosya okunuyor…");
        try
        {
            var result = await analyzeHandler.HandleAsync(new AnalyzeCargoImportRequest
            {
                FilePath  = filePath,
                Direction = Direction,
                Progress  = new Progress<ImportProgress>(ReportProgress)
            });

            if (!result.Success)
                return result.ErrorMessage ?? "Dosya analiz edilemedi.";

            Analysis = result.Data!;

            LoadRows(
                Analysis.Rows.Select(dto => new CargoImportRowItem(dto, OnRowInclusionChanged)).ToList(),
                total:          Analysis.Rows.Count,
                valid:          Analysis.ValidCount,
                warning:        Analysis.WarningCount,
                error:          Analysis.ErrorCount,
                duplicate:      Analysis.DuplicateCount,
                skippedEmpty:   Analysis.SkippedEmptyRows,
                ignoredColumns: Analysis.IgnoredColumns);

            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<OperationResult<ImportResult>> ImportAsync()
    {
        var selected = IncludedItems.Select(r => r.Dto).ToList();

        IsBusy = true;
        try
        {
            var result = await importHandler.HandleAsync(new ImportCargoShipmentsRequest
            {
                Direction              = Direction,
                SourceName             = Analysis!.SourceName,
                Rows                   = selected,
                CreatedByUserId        = userContext.UserId,
                AnalysisTotalRows      = Analysis.Rows.Count,
                AnalysisValidCount     = Analysis.ValidCount,
                AnalysisWarningCount   = Analysis.WarningCount,
                AnalysisErrorCount     = Analysis.ErrorCount,
                AnalysisDuplicateCount = Analysis.DuplicateCount,
                Progress               = new Progress<ImportProgress>(ReportProgress)
            });

            if (result.Success)
                LastResult = result.Data;

            return result;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// Önizleme satırının kargo görünümü.
///
/// Durum simgesi, mesajlar ve "dahil et" seçimi ortak tabandadır; burada
/// yalnızca KARGO KOLONLARI var.
/// </summary>
public class CargoImportRowItem(CargoImportRowDto dto, Action inclusionChanged)
    : ImportRowItemBase(dto, inclusionChanged)
{
    /// <summary>Tabandaki satırın kargoya özgü tipi — istek kurarken gerekiyor.</summary>
    public CargoImportRowDto Dto { get; } = dto;

    public string DateDisplay     => Dto.ShipmentDate == default ? "—" : Dto.ShipmentDate.ToString("dd.MM.yyyy");
    public string CompanyDisplay  => Dto.CompanyName ?? "—";
    public string CargoDisplay    => Dto.CargoCompanyName ?? "—";
    public string TrackingDisplay => Dto.TrackingNumber ?? "—";

    public string PriorityDisplay => Dto.Priority switch
    {
        CargoShipmentPriority.Medium   => "Orta",
        CargoShipmentPriority.Urgent   => "Acil",
        CargoShipmentPriority.Critical => "Çok Acil",
        _                              => "Normal"
    };
}
