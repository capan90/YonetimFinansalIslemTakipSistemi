using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;

/// <summary>
/// Excel içe aktarma sihirbazının durumu: Dosya Seç → Önizleme → İçe Aktarma → Sonuç.
/// Dosya diyalogları ve panel görünürlüğü window code-behind'dadır; bu sınıf
/// yalnızca veri, filtre ve handler orkestrasyonunu yönetir.
/// </summary>
public class CargoImportViewModel : INotifyPropertyChanged
{
    private readonly AnalyzeCargoImportHandler   _analyzeHandler;
    private readonly ImportCargoShipmentsHandler _importHandler;
    private readonly IUserContext                _userContext;

    public CargoShipmentDirection Direction { get; }

    public CargoImportViewModel(
        AnalyzeCargoImportHandler   analyzeHandler,
        ImportCargoShipmentsHandler importHandler,
        IUserContext                userContext,
        CargoShipmentDirection      direction)
    {
        _analyzeHandler = analyzeHandler;
        _importHandler  = importHandler;
        _userContext    = userContext;
        Direction       = direction;
    }

    // ── İlerleme ────────────────────────────────────────────────────────────

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; OnPropertyChanged(); }
    }

    private string _progressText = string.Empty;
    public string ProgressText
    {
        get => _progressText;
        private set { _progressText = value; OnPropertyChanged(); }
    }

    private int _progressValue;
    public int ProgressValue
    {
        get => _progressValue;
        private set { _progressValue = value; OnPropertyChanged(); }
    }

    private int _progressMax = 1;
    public int ProgressMax
    {
        get => _progressMax;
        private set { _progressMax = value; OnPropertyChanged(); }
    }

    private bool _progressIndeterminate;
    public bool ProgressIndeterminate
    {
        get => _progressIndeterminate;
        private set { _progressIndeterminate = value; OnPropertyChanged(); }
    }

    private void ReportProgress(ImportProgress p)
    {
        // "Veritabanına kaydediliyor" tek transaction'dır — satır bazlı ölçülemez
        ProgressIndeterminate = p.Phase.StartsWith("Veritabanına", StringComparison.Ordinal);
        ProgressText  = $"{p.Phase}… ({p.Processed}/{p.Total})";
        ProgressValue = p.Processed;
        ProgressMax   = Math.Max(1, p.Total);
    }

    // ── Analiz + önizleme ───────────────────────────────────────────────────

    public CargoImportAnalysisResult? Analysis { get; private set; }

    public ObservableCollection<CargoImportRowItem> FilteredRows { get; } = [];
    private List<CargoImportRowItem> _allRows = [];

    public IReadOnlyList<string> FilterOptions { get; private set; } = [];

    private string? _selectedFilter;
    public string? SelectedFilter
    {
        get => _selectedFilter;
        set { _selectedFilter = value; OnPropertyChanged(); ApplyFilter(); }
    }

    private string _analysisSummary = string.Empty;
    public string AnalysisSummary
    {
        get => _analysisSummary;
        private set { _analysisSummary = value; OnPropertyChanged(); }
    }

    private int _selectedCount;
    public int SelectedCount
    {
        get => _selectedCount;
        private set
        {
            _selectedCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanImport));
            OnPropertyChanged(nameof(ImportButtonText));
        }
    }

    public bool CanImport => SelectedCount > 0 && !IsBusy;
    public string ImportButtonText => $"Devam Et ({SelectedCount} kayıt)";

    /// <summary>Dosyayı analiz eder ve önizleme koleksiyonlarını doldurur. Hata → mesaj döner, null = başarı.</summary>
    public async Task<string?> AnalyzeAsync(string filePath)
    {
        IsBusy = true;
        ProgressIndeterminate = true;
        ProgressText = "Dosya okunuyor…";
        try
        {
            var result = await _analyzeHandler.HandleAsync(new AnalyzeCargoImportRequest
            {
                FilePath  = filePath,
                Direction = Direction,
                Progress  = new Progress<ImportProgress>(ReportProgress)
            });

            if (!result.Success)
                return result.ErrorMessage ?? "Dosya analiz edilemedi.";

            Analysis = result.Data!;
            _allRows = Analysis.Rows
                .Select(dto => new CargoImportRowItem(dto, OnRowInclusionChanged))
                .ToList();

            BuildFilterOptions();
            SelectedFilter = FilterOptions[0]; // ApplyFilter tetikler
            RecountSelected();

            var parts = new List<string>
            {
                $"{Analysis.Rows.Count} satır okundu",
                $"{Analysis.ValidCount} geçerli",
                $"{Analysis.WarningCount} uyarılı",
                $"{Analysis.ErrorCount} hatalı",
                $"{Analysis.DuplicateCount} mükerrer"
            };
            if (Analysis.SkippedEmptyRows > 0) parts.Add($"{Analysis.SkippedEmptyRows} boş satır atlandı");
            if (Analysis.IgnoredColumns.Count > 0) parts.Add($"yok sayılan kolonlar: {string.Join(", ", Analysis.IgnoredColumns)}");
            AnalysisSummary = string.Join(" • ", parts);

            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildFilterOptions()
    {
        FilterOptions =
        [
            $"Tümü ({Analysis!.Rows.Count})",
            $"✔ Geçerli ({Analysis.ValidCount})",
            $"⚠ Uyarılı ({Analysis.WarningCount})",
            $"❌ Hatalı ({Analysis.ErrorCount})",
            $"🔁 Mükerrer ({Analysis.DuplicateCount})"
        ];
        OnPropertyChanged(nameof(FilterOptions));
    }

    private void ApplyFilter()
    {
        // Filtre seçenek metni "✔ Geçerli (35)" biçimindedir — durum adından ayrıştırılır
        CargoImportRowStatus? status = _selectedFilter switch
        {
            var s when s?.Contains("Geçerli",  StringComparison.Ordinal) == true => CargoImportRowStatus.Valid,
            var s when s?.Contains("Uyarılı",  StringComparison.Ordinal) == true => CargoImportRowStatus.Warning,
            var s when s?.Contains("Hatalı",   StringComparison.Ordinal) == true => CargoImportRowStatus.Error,
            var s when s?.Contains("Mükerrer", StringComparison.Ordinal) == true => CargoImportRowStatus.Duplicate,
            _ => null
        };

        FilteredRows.Clear();
        foreach (var row in _allRows.Where(r => status is null || r.Dto.Status == status))
            FilteredRows.Add(row);
    }

    private void OnRowInclusionChanged() => RecountSelected();

    private void RecountSelected() => SelectedCount = _allRows.Count(r => r.Included);

    // ── İçe aktarma ─────────────────────────────────────────────────────────

    public ImportResult? LastResult { get; private set; }

    public async Task<OperationResult<ImportResult>> ImportAsync()
    {
        var selected = _allRows.Where(r => r.Included).Select(r => r.Dto).ToList();

        IsBusy = true;
        try
        {
            var result = await _importHandler.HandleAsync(new ImportCargoShipmentsRequest
            {
                Direction              = Direction,
                SourceName             = Analysis!.SourceName,
                Rows                   = selected,
                CreatedByUserId        = _userContext.UserId,
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

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Önizleme satırının grid modeli — durum simgesi, mesajlar ve dahil et seçimi.</summary>
public class CargoImportRowItem : INotifyPropertyChanged
{
    private readonly Action _inclusionChanged;

    public CargoImportRowDto Dto { get; }

    public CargoImportRowItem(CargoImportRowDto dto, Action inclusionChanged)
    {
        Dto               = dto;
        _inclusionChanged = inclusionChanged;
        _included         = dto.IncludedByDefault;
    }

    private bool _included;
    public bool Included
    {
        get => _included;
        set
        {
            if (!Dto.CanInclude) return; // hatalı/kesin mükerrer asla dahil edilemez
            _included = value;
            OnPropertyChanged();
            _inclusionChanged();
        }
    }

    public bool CanToggle => Dto.CanInclude;

    public int RowNumber => Dto.RowNumber;

    public string StatusText => Dto.Status switch
    {
        CargoImportRowStatus.Valid     => "Geçerli",
        CargoImportRowStatus.Warning   => "Uyarılı",
        CargoImportRowStatus.Error     => "Hatalı",
        CargoImportRowStatus.Duplicate => "Mükerrer",
        _                              => Dto.Status.ToString()
    };

    public string StatusGlyph => Dto.Status switch
    {
        CargoImportRowStatus.Valid     => "✔",
        CargoImportRowStatus.Warning   => "⚠",
        CargoImportRowStatus.Error     => "❌",
        CargoImportRowStatus.Duplicate => "🔁",
        _                              => "?"
    };

    public string DateDisplay      => Dto.ShipmentDate == default ? "—" : Dto.ShipmentDate.ToString("dd.MM.yyyy");
    public string CompanyDisplay   => Dto.CompanyName ?? "—";
    public string CargoDisplay     => Dto.CargoCompanyName ?? "—";
    public string TrackingDisplay  => Dto.TrackingNumber ?? "—";
    public string PriorityDisplay  => Dto.Priority switch
    {
        Domain.Enums.CargoShipmentPriority.Medium   => "Orta",
        Domain.Enums.CargoShipmentPriority.Urgent   => "Acil",
        Domain.Enums.CargoShipmentPriority.Critical => "Çok Acil",
        _                                           => "Normal"
    };

    /// <summary>Tüm doğrulama mesajları + mükerrer açıklaması, tek metin (grid ve tooltip).</summary>
    public string MessagesText
    {
        get
        {
            var parts = Dto.Messages.Select(m => $"{m.Column}: {m.Message}").ToList();
            if (Dto.DuplicateReason is not null) parts.Add(Dto.DuplicateReason.Description);
            return parts.Count == 0 ? string.Empty : string.Join("  |  ", parts);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
