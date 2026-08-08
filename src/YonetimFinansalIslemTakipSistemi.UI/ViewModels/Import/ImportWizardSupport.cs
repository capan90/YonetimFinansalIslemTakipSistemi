using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Import;

/// <summary>
/// İçe aktarma önizleme satırının ortak grid modeli: durum simgesi, mesajlar,
/// dahil et seçimi. Alana özgü görüntü kolonları türeyen sınıflarda tanımlanır.
///
/// Dört sihirbazın DÖRDÜ de bu tabanı kullanır (Faz F3'te kargo da geçti).
/// </summary>
public abstract class ImportRowItemBase : INotifyPropertyChanged
{
    private readonly Action _inclusionChanged;

    protected ImportRowItemBase(ImportRowBase row, Action inclusionChanged)
    {
        Row               = row;
        _inclusionChanged = inclusionChanged;
        _included         = row.IncludedByDefault;
    }

    public ImportRowBase Row { get; }

    private bool _included;
    public bool Included
    {
        get => _included;
        set
        {
            if (!Row.CanInclude) return; // hatalı/kesin mükerrer asla dahil edilemez
            _included = value;
            OnPropertyChanged();
            _inclusionChanged();
        }
    }

    public bool CanToggle => Row.CanInclude;
    public int RowNumber => Row.RowNumber;

    public string StatusText => Row.Status switch
    {
        CargoImportRowStatus.Valid     => "Geçerli",
        CargoImportRowStatus.Warning   => "Uyarılı",
        CargoImportRowStatus.Error     => "Hatalı",
        CargoImportRowStatus.Duplicate => "Mükerrer",
        _                              => Row.Status.ToString()
    };

    public string StatusGlyph => Row.Status switch
    {
        CargoImportRowStatus.Valid     => "✔",
        CargoImportRowStatus.Warning   => "⚠",
        CargoImportRowStatus.Error     => "❌",
        CargoImportRowStatus.Duplicate => "🔁",
        _                              => "?"
    };

    public string MessagesText
    {
        get
        {
            var parts = Row.Messages.Select(m => $"{m.Column}: {m.Message}").ToList();
            if (Row.DuplicateReason is not null) parts.Add(Row.DuplicateReason.Description);
            return parts.Count == 0 ? string.Empty : string.Join("  |  ", parts);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// İçe aktarma sihirbazı VM tabanı: ilerleme, durum filtresi, özet sayılar ve
/// seçim yönetimi. Analiz/import orkestrasyonu türeyen sınıflardadır.
/// </summary>
public abstract class ImportWizardViewModelBase<TItem> : INotifyPropertyChanged
    where TItem : ImportRowItemBase
{
    // ── İlerleme ────────────────────────────────────────────────────────────

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        protected set
        {
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanImport));
        }
    }

    private string _progressText = string.Empty;
    public string ProgressText { get => _progressText; private set { _progressText = value; OnPropertyChanged(); } }

    private int _progressValue;
    public int ProgressValue { get => _progressValue; private set { _progressValue = value; OnPropertyChanged(); } }

    private int _progressMax = 1;
    public int ProgressMax { get => _progressMax; private set { _progressMax = value; OnPropertyChanged(); } }

    private bool _progressIndeterminate;
    public bool ProgressIndeterminate
    {
        get => _progressIndeterminate;
        protected set { _progressIndeterminate = value; OnPropertyChanged(); }
    }

    protected void SetProgressText(string text) { ProgressText = text; }

    protected void ReportProgress(ImportProgress p)
    {
        // Tek transaction'lık kayıt aşaması satır bazlı ölçülemez
        ProgressIndeterminate = p.Phase.StartsWith("Veritabanına", StringComparison.Ordinal);
        ProgressText  = $"{p.Phase}… ({p.Processed}/{p.Total})";
        ProgressValue = p.Processed;
        ProgressMax   = Math.Max(1, p.Total);
    }

    // ── Önizleme + filtre ───────────────────────────────────────────────────

    public ObservableCollection<TItem> FilteredRows { get; } = [];
    private List<TItem> _allRows = [];

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

    /// <summary>Analiz sonucunu önizleme koleksiyonlarına yükler ve özet metnini kurar.</summary>
    protected void LoadRows(List<TItem> items,
        int total, int valid, int warning, int error, int duplicate,
        int skippedEmpty, IReadOnlyList<string> ignoredColumns)
    {
        _allRows = items;

        FilterOptions =
        [
            $"Tümü ({total})",
            $"✔ Geçerli ({valid})",
            $"⚠ Uyarılı ({warning})",
            $"❌ Hatalı ({error})",
            $"🔁 Mükerrer ({duplicate})"
        ];
        OnPropertyChanged(nameof(FilterOptions));
        SelectedFilter = FilterOptions[0]; // ApplyFilter tetikler
        RecountSelected();

        var parts = new List<string>
        {
            $"{total} satır okundu", $"{valid} geçerli", $"{warning} uyarılı",
            $"{error} hatalı", $"{duplicate} mükerrer"
        };
        if (skippedEmpty > 0) parts.Add($"{skippedEmpty} boş satır atlandı");
        if (ignoredColumns.Count > 0) parts.Add($"yok sayılan kolonlar: {string.Join(", ", ignoredColumns)}");
        AnalysisSummary = string.Join(" • ", parts);
    }

    private void ApplyFilter()
    {
        CargoImportRowStatus? status = _selectedFilter switch
        {
            var s when s?.Contains("Geçerli",  StringComparison.Ordinal) == true => CargoImportRowStatus.Valid,
            var s when s?.Contains("Uyarılı",  StringComparison.Ordinal) == true => CargoImportRowStatus.Warning,
            var s when s?.Contains("Hatalı",   StringComparison.Ordinal) == true => CargoImportRowStatus.Error,
            var s when s?.Contains("Mükerrer", StringComparison.Ordinal) == true => CargoImportRowStatus.Duplicate,
            _ => null
        };

        FilteredRows.Clear();
        foreach (var row in _allRows.Where(r => status is null || r.Row.Status == status))
            FilteredRows.Add(row);
    }

    protected void OnRowInclusionChanged() => RecountSelected();
    private void RecountSelected() => SelectedCount = _allRows.Count(r => r.Included);

    protected IReadOnlyList<TItem> IncludedItems => _allRows.Where(r => r.Included).ToList();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
