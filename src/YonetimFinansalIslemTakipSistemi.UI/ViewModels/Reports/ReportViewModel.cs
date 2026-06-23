using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.Reports.Queries.GetReport;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Domain.Extensions;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Reports;

public class ReportViewModel : INotifyPropertyChanged
{
    private readonly GetReportHandler _handler;
    private readonly IDialogService   _dialogService;

    private DateTime? _startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime? _endDate   = DateTime.Today;
    private bool      _isLoading;
    private string?   _errorMessage;

    private string?   _selectedTransactionType;
    private string?   _selectedCurrencyType;
    private string?   _descriptionFilter;
    private bool      _showTransactionDetails;

    // Son başarılı yükleme sonucu — önizleme ve export için önbellek
    public ReportDto? LastReportDto { get; private set; }

    private bool _hasReport;
    public bool HasReport
    {
        get => _hasReport;
        private set { _hasReport = value; OnPropertyChanged(); }
    }

    // Para birimi özet kartları — her zaman 3 nesne; boşken sıfırlı
    private CurrencySummaryDto _trySummary = EmptySummary(CurrencyType.TRY, "TL");
    private CurrencySummaryDto _usdSummary = EmptySummary(CurrencyType.USD, "USD");
    private CurrencySummaryDto _eurSummary = EmptySummary(CurrencyType.EUR, "EUR");

    public DateTime? StartDate
    {
        get => _startDate;
        set { _startDate = value; OnPropertyChanged(); }
    }

    public DateTime? EndDate
    {
        get => _endDate;
        set { _endDate = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    // ── Filtreler ──────────────────────────────────────────────────────────────

    public IReadOnlyList<string> TransactionTypeOptions { get; } =
        new[] { "Tümü", "Giriş", "Çıkış" };

    public IReadOnlyList<string> CurrencyTypeOptions { get; } =
        new[] { "Tümü", "TRY", "USD", "EUR" };

    public string? SelectedTransactionType
    {
        get => _selectedTransactionType;
        set { _selectedTransactionType = value; OnPropertyChanged(); }
    }

    public string? SelectedCurrencyType
    {
        get => _selectedCurrencyType;
        set { _selectedCurrencyType = value; OnPropertyChanged(); }
    }

    public string? DescriptionFilter
    {
        get => _descriptionFilter;
        set { _descriptionFilter = value; OnPropertyChanged(); }
    }

    /// <summary>True ise rapor detay satırları da yüklenir ve gösterilir.</summary>
    public bool ShowTransactionDetails
    {
        get => _showTransactionDetails;
        set { _showTransactionDetails = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasDetails)); }
    }

    // ── Para birimi özet kartları ──────────────────────────────────────────────

    public CurrencySummaryDto TrySummary
    {
        get => _trySummary;
        private set { _trySummary = value; OnPropertyChanged(); }
    }

    public CurrencySummaryDto UsdSummary
    {
        get => _usdSummary;
        private set { _usdSummary = value; OnPropertyChanged(); }
    }

    public CurrencySummaryDto EurSummary
    {
        get => _eurSummary;
        private set { _eurSummary = value; OnPropertyChanged(); }
    }

    // ── İşlem türü özet tablosu ────────────────────────────────────────────────

    /// <summary>
    /// DataGrid için düzleştirilmiş işlem türü satırları.
    /// Giriş → Alacak kolonuna, Çıkış → Borç kolonuna yazılır.
    /// </summary>
    public ObservableCollection<TransactionTypeRow> TypeRows { get; } = new();

    // ── Detay satırları ────────────────────────────────────────────────────────

    /// <summary>ShowTransactionDetails=true ve veri varsa dolu. DataGrid kaynağı.</summary>
    public ObservableCollection<TransactionDetailRow> DetailRows { get; } = new();

    /// <summary>Detay DataGrid görünürlüğü için binding hedefi.</summary>
    public bool HasDetails => ShowTransactionDetails && DetailRows.Count > 0;

    // ── Komutlar ───────────────────────────────────────────────────────────────

    public ICommand LoadReportCommand { get; }

    public ReportViewModel(GetReportHandler handler, IDialogService dialogService)
    {
        _handler       = handler;
        _dialogService = dialogService;
        LoadReportCommand = new RelayCommand(async () => await LoadAsync(), () => !_isLoading);
    }

    public async Task LoadAsync()
    {
        if (_isLoading) return;

        IsLoading    = true;
        ErrorMessage = null;

        try
        {
            var result = await _handler.HandleAsync(new GetReportQuery
            {
                StartDate              = StartDate,
                EndDate                = EndDate,
                TransactionType        = ParseTransactionType(SelectedTransactionType),
                CurrencyType           = ParseCurrencyType(SelectedCurrencyType),
                DescriptionContains    = string.IsNullOrWhiteSpace(DescriptionFilter) ? null : DescriptionFilter.Trim(),
                ShowTransactionDetails = ShowTransactionDetails
            });

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }

            var dto = result.Data!;

            LastReportDto = dto;
            HasReport     = true;

            // Para birimi kartlarını güncelle (filtre uygulanmış veriyle)
            TrySummary = dto.CurrencySummaries.FirstOrDefault(c => c.Currency == CurrencyType.TRY)
                         ?? EmptySummary(CurrencyType.TRY, "TL");
            UsdSummary = dto.CurrencySummaries.FirstOrDefault(c => c.Currency == CurrencyType.USD)
                         ?? EmptySummary(CurrencyType.USD, "USD");
            EurSummary = dto.CurrencySummaries.FirstOrDefault(c => c.Currency == CurrencyType.EUR)
                         ?? EmptySummary(CurrencyType.EUR, "EUR");

            // İşlem türü özet tablosu — Giriş→Alacak, Çıkış→Borç mantığıyla düzleştir
            TypeRows.Clear();
            foreach (var ts in dto.TransactionTypeSummaries)
            {
                decimal tryAmt = 0, usdAmt = 0, eurAmt = 0;
                int     tryCnt = 0, usdCnt = 0, eurCnt = 0;

                foreach (var ca in ts.AmountsByCurrency)
                {
                    switch (ca.Currency)
                    {
                        case CurrencyType.TRY: tryAmt = ca.TotalAmount; tryCnt = ca.Count; break;
                        case CurrencyType.USD: usdAmt = ca.TotalAmount; usdCnt = ca.Count; break;
                        case CurrencyType.EUR: eurAmt = ca.TotalAmount; eurCnt = ca.Count; break;
                    }
                }

                // Giriş = Alacak, Çıkış = Borç
                var isInflow = ts.TransactionType.GetFinancialDirection() == FinancialDirection.Inflow;
                TypeRows.Add(new TransactionTypeRow(
                    ts.TypeDisplay,
                    TryBorc:   isInflow ? 0m     : tryAmt,
                    TryAlacak: isInflow ? tryAmt : 0m,
                    TryCount:  tryCnt,
                    UsdBorc:   isInflow ? 0m     : usdAmt,
                    UsdAlacak: isInflow ? usdAmt : 0m,
                    UsdCount:  usdCnt,
                    EurBorc:   isInflow ? 0m     : eurAmt,
                    EurAlacak: isInflow ? eurAmt : 0m,
                    EurCount:  eurCnt));
            }

            // Detay satırları
            DetailRows.Clear();
            if (dto.TransactionDetails is not null)
            {
                foreach (var d in dto.TransactionDetails)
                {
                    DetailRows.Add(new TransactionDetailRow(
                        d.TransactionDate.ToString("dd.MM.yyyy"),
                        d.Description,
                        d.TypeDisplay,
                        d.CurrencyDisplay,
                        d.Borc,
                        d.Alacak,
                        d.Balance));
                }
            }
            OnPropertyChanged(nameof(HasDetails));
        }
        catch
        {
            _dialogService.ShowError("Rapor yüklenirken beklenmeyen bir hata oluştu.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static CurrencySummaryDto EmptySummary(CurrencyType currency, string display)
        => new() { Currency = currency, CurrencyDisplay = display };

    private static TransactionType? ParseTransactionType(string? display) => display switch
    {
        "Giriş" => TransactionType.Giris,
        "Çıkış" => TransactionType.Cikis,
        _       => null
    };

    private static CurrencyType? ParseCurrencyType(string? display) => display switch
    {
        "TRY" => CurrencyType.TRY,
        "USD" => CurrencyType.USD,
        "EUR" => CurrencyType.EUR,
        _     => null
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// DataGrid bağlaması için düzleştirilmiş işlem türü satırı.
/// Giriş işlemi tutarları Alacak, Çıkış işlemi tutarları Borç kolonuna yazılır.
/// </summary>
public record TransactionTypeRow(
    string  TypeDisplay,
    decimal TryBorc,   decimal TryAlacak,  int TryCount,
    decimal UsdBorc,   decimal UsdAlacak,  int UsdCount,
    decimal EurBorc,   decimal EurAlacak,  int EurCount);

/// <summary>
/// Detay DataGrid için satır modeli.
/// Balance = para birimi bazlı kümülatif bakiye (rapor döneminin başından itibaren).
/// </summary>
public record TransactionDetailRow(
    string  Date,
    string  Description,
    string  TypeDisplay,
    string  CurrencyDisplay,
    decimal Borc,
    decimal Alacak,
    decimal Balance);
