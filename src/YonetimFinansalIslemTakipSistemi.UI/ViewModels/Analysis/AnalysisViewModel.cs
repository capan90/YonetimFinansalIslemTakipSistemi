using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.Analysis.Queries.GetDashboard;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Analysis;

public class AnalysisViewModel : INotifyPropertyChanged
{
    private readonly GetDashboardHandler _handler;

    public AnalysisViewModel(GetDashboardHandler handler)
    {
        _handler     = handler;
        LoadCommand  = new RelayCommand(async () => await LoadAsync());
    }

    // ── Filtreler ─────────────────────────────────────────────────────────────

    private DateTime? _startDate;
    public DateTime? StartDate
    {
        get => _startDate;
        set { _startDate = value; OnPropertyChanged(); }
    }

    private DateTime? _endDate;
    public DateTime? EndDate
    {
        get => _endDate;
        set { _endDate = value; OnPropertyChanged(); }
    }

    public IReadOnlyList<string> CurrencyTypeOptions  { get; } = ["Tümü", "TRY", "USD", "EUR"];
    public IReadOnlyList<string> TransactionTypeOptions { get; } = ["Tümü", "Giriş", "Çıkış"];

    private string? _selectedCurrencyType;
    public string? SelectedCurrencyType
    {
        get => _selectedCurrencyType;
        set { _selectedCurrencyType = value; OnPropertyChanged(); }
    }

    private string? _selectedTransactionType;
    public string? SelectedTransactionType
    {
        get => _selectedTransactionType;
        set { _selectedTransactionType = value; OnPropertyChanged(); }
    }

    private string? _descriptionFilter;
    public string? DescriptionFilter
    {
        get => _descriptionFilter;
        set { _descriptionFilter = value; OnPropertyChanged(); }
    }

    // ── Durum ─────────────────────────────────────────────────────────────────

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    // ── Özet Kartları ─────────────────────────────────────────────────────────

    private decimal _totalAlacak;
    public decimal TotalAlacak { get => _totalAlacak; set { _totalAlacak = value; OnPropertyChanged(); } }

    private decimal _totalBorc;
    public decimal TotalBorc { get => _totalBorc; set { _totalBorc = value; OnPropertyChanged(); } }

    private decimal _netFark;
    public decimal NetFark { get => _netFark; set { _netFark = value; OnPropertyChanged(); } }

    private int _totalCount;
    public int TotalCount { get => _totalCount; set { _totalCount = value; OnPropertyChanged(); } }

    private decimal _maxGiris;
    public decimal MaxGiris { get => _maxGiris; set { _maxGiris = value; OnPropertyChanged(); } }

    private decimal _maxCikis;
    public decimal MaxCikis { get => _maxCikis; set { _maxCikis = value; OnPropertyChanged(); } }

    // ── Para Birimi Kartları ──────────────────────────────────────────────────

    public ObservableCollection<DashboardCurrencyCardDto> CurrencyCards { get; } = new();

    // ── Günlük Trend ─────────────────────────────────────────────────────────

    public ObservableCollection<DailyTrendDto> DailyTrend { get; } = new();

    // ── Son İşlemler ──────────────────────────────────────────────────────────

    public ObservableCollection<RecentTransactionDto> RecentTransactions { get; } = new();

    // ── Komutlar ──────────────────────────────────────────────────────────────

    public ICommand LoadCommand { get; }

    public async Task LoadAsync()
    {
        IsLoading    = true;
        ErrorMessage = null;

        try
        {
            var query = new GetDashboardQuery
            {
                StartDate           = StartDate,
                EndDate             = EndDate,
                CurrencyType        = ParseCurrency(SelectedCurrencyType),
                TransactionType     = ParseTransactionType(SelectedTransactionType),
                DescriptionContains = DescriptionFilter
            };

            var result = await _handler.HandleAsync(query);

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }

            var dto = result.Data!;

            TotalAlacak = dto.TotalAlacak;
            TotalBorc   = dto.TotalBorc;
            NetFark     = dto.NetFark;
            TotalCount  = dto.TotalCount;
            MaxGiris    = dto.MaxGiris;
            MaxCikis    = dto.MaxCikis;

            CurrencyCards.Clear();
            foreach (var c in dto.CurrencyCards) CurrencyCards.Add(c);

            DailyTrend.Clear();
            foreach (var d in dto.DailyTrend) DailyTrend.Add(d);

            RecentTransactions.Clear();
            foreach (var r in dto.RecentTransactions) RecentTransactions.Add(r);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static CurrencyType? ParseCurrency(string? s) => s switch
    {
        "TRY" => CurrencyType.TRY,
        "USD" => CurrencyType.USD,
        "EUR" => CurrencyType.EUR,
        _     => null
    };

    private static TransactionType? ParseTransactionType(string? s) => s switch
    {
        "Giriş" => TransactionType.Giris,
        "Çıkış" => TransactionType.Cikis,
        _       => null
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
