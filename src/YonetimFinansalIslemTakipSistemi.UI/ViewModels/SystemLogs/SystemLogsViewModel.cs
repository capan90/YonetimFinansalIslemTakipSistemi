using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.SystemLogs;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.SystemLogs;

public class SystemLogsViewModel : INotifyPropertyChanged
{
    private readonly ISystemLogService _logService;
    private readonly IUserContext      _userContext;

    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private string    _selectedLevel    = "(Tümü)";
    private string    _selectedCategory = "(Tümü)";
    private string    _selectedStatus   = "(Tümü)";
    private string    _searchText       = string.Empty;
    private int       _currentPage      = 1;
    private int       _totalPages       = 1;
    private int       _totalCount;
    private bool      _isLoading;

    private SystemLogListItemDto? _selected;
    private ObservableCollection<SystemLogListItemDto> _items = [];

    // ── Özet sayaçlar ────────────────────────────────────────────────────
    public int TodayCriticalCount  { get; private set; }
    public int TodayErrorCount     { get; private set; }
    public int Last24hWarningCount { get; private set; }
    public int OpenCriticalCount   { get; private set; }

    // ── Filtreler ────────────────────────────────────────────────────────
    public DateTime? DateFrom
    {
        get => _dateFrom;
        set { _dateFrom = value; OnPropertyChanged(); }
    }

    public DateTime? DateTo
    {
        get => _dateTo;
        set { _dateTo = value; OnPropertyChanged(); }
    }

    public string SelectedLevel
    {
        get => _selectedLevel;
        set { _selectedLevel = value; OnPropertyChanged(); }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set { _selectedCategory = value; OnPropertyChanged(); }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set { _selectedStatus = value; OnPropertyChanged(); }
    }

    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); }
    }

    // ── Liste ─────────────────────────────────────────────────────────────
    public ObservableCollection<SystemLogListItemDto> Items
    {
        get => _items;
        private set { _items = value; OnPropertyChanged(); }
    }

    public SystemLogListItemDto? Selected
    {
        get => _selected;
        set { _selected = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSelected)); }
    }

    public bool HasSelected => _selected is not null;

    // ── Sayfalama ─────────────────────────────────────────────────────────
    public int CurrentPage
    {
        get => _currentPage;
        private set { _currentPage = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanGoPrev)); OnPropertyChanged(nameof(CanGoNext)); }
    }

    public int TotalPages
    {
        get => _totalPages;
        private set { _totalPages = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanGoPrev)); OnPropertyChanged(nameof(CanGoNext)); OnPropertyChanged(nameof(PageInfo)); }
    }

    public int  TotalCount { get => _totalCount; private set { _totalCount = value; OnPropertyChanged(); } }
    public bool CanGoPrev  => _currentPage > 1;
    public bool CanGoNext  => _currentPage < _totalPages;
    public string PageInfo => $"Sayfa {_currentPage} / {_totalPages}  ({_totalCount} kayıt)";

    public bool IsLoading
    {
        get => _isLoading;
        private set { _isLoading = value; OnPropertyChanged(); }
    }

    // ── Filtre seçenekleri ────────────────────────────────────────────────
    public IReadOnlyList<string> LevelOptions { get; } =
        ["(Tümü)", "Bilgi", "Uyarı", "Hata", "Kritik"];

    public IReadOnlyList<string> CategoryOptions { get; } =
        ["(Tümü)", "Startup", "Database", "Mail", "Cargo", "Finance", "UI", "Auth", "Report", "Unknown"];

    public IReadOnlyList<string> StatusOptions { get; } =
        ["(Tümü)", "Açık", "Çözüldü"];

    // ── Komutlar ─────────────────────────────────────────────────────────
    public ICommand SearchCommand { get; }
    public ICommand ClearCommand  { get; }
    public ICommand PrevCommand   { get; }
    public ICommand NextCommand   { get; }

    public SystemLogsViewModel(ISystemLogService logService, IUserContext userContext)
    {
        _logService  = logService;
        _userContext = userContext;

        SearchCommand = new RelayCommand(async () => { CurrentPage = 1; await LoadAsync(); });
        ClearCommand  = new RelayCommand(async () => { ClearFilters(); CurrentPage = 1; await LoadAsync(); });
        PrevCommand   = new RelayCommand(async () => { if (CanGoPrev) { CurrentPage--; await LoadAsync(); } });
        NextCommand   = new RelayCommand(async () => { if (CanGoNext) { CurrentPage++; await LoadAsync(); } });
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var query = BuildQuery();
            var result = await _logService.SearchAsync(query);

            Items      = new ObservableCollection<SystemLogListItemDto>(result.Items);
            TotalCount = result.TotalCount;
            TotalPages = Math.Max(1, result.TotalPages);
            CurrentPage = result.Page;

            await LoadSummaryCountsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task MarkSelectedResolvedAsync(string? note)
    {
        if (Selected is null) return;
        await _logService.MarkResolvedAsync(Selected.Id, _userContext.UserId, note);
        await LoadAsync();
    }

    private async Task LoadSummaryCountsAsync()
    {
        var today     = DateTime.Today;
        var yesterday = today.AddDays(-1);

        var todayCritResult  = await _logService.SearchAsync(new SystemLogSearchQuery { DateFrom = today, Level = SystemLogLevel.Critical, PageSize = 1 });
        var todayErrResult   = await _logService.SearchAsync(new SystemLogSearchQuery { DateFrom = today, Level = SystemLogLevel.Error,    PageSize = 1 });
        var last24hWarnResult = await _logService.SearchAsync(new SystemLogSearchQuery { DateFrom = yesterday, Level = SystemLogLevel.Warning, PageSize = 1 });
        var openCritResult   = await _logService.SearchAsync(new SystemLogSearchQuery { Level = SystemLogLevel.Critical, IsResolved = false, PageSize = 1 });

        TodayCriticalCount  = todayCritResult.TotalCount;
        TodayErrorCount     = todayErrResult.TotalCount;
        Last24hWarningCount = last24hWarnResult.TotalCount;
        OpenCriticalCount   = openCritResult.TotalCount;

        OnPropertyChanged(nameof(TodayCriticalCount));
        OnPropertyChanged(nameof(TodayErrorCount));
        OnPropertyChanged(nameof(Last24hWarningCount));
        OnPropertyChanged(nameof(OpenCriticalCount));
    }

    private SystemLogSearchQuery BuildQuery() => new()
    {
        DateFrom   = DateFrom,
        DateTo     = DateTo,
        Level      = ParseLevel(SelectedLevel),
        Category   = SelectedCategory == "(Tümü)" ? null : SelectedCategory,
        IsResolved = SelectedStatus switch { "Açık" => false, "Çözüldü" => true, _ => null },
        SearchText = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
        Page       = CurrentPage,
        PageSize   = 50
    };

    private void ClearFilters()
    {
        DateFrom         = null;
        DateTo           = null;
        SelectedLevel    = "(Tümü)";
        SelectedCategory = "(Tümü)";
        SelectedStatus   = "(Tümü)";
        SearchText       = string.Empty;
    }

    private static SystemLogLevel? ParseLevel(string display) => display switch
    {
        "Bilgi"   => SystemLogLevel.Info,
        "Uyarı"   => SystemLogLevel.Warning,
        "Hata"    => SystemLogLevel.Error,
        "Kritik"  => SystemLogLevel.Critical,
        _         => null
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
