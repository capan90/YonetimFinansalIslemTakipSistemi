using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.Reports.Queries.GetReport;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
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

    /// <summary>
    /// DataGrid için düzleştirilmiş işlem türü satırları.
    /// Her para birimi ayrı sütun olarak gösterilir; tek rakamda birleştirilmez.
    /// </summary>
    public ObservableCollection<TransactionTypeRow> TypeRows { get; } = new();

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
                StartDate = StartDate,
                EndDate   = EndDate
            });

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage;
                return;
            }

            var dto = result.Data!;

            // Para birimi kartlarını güncelle
            LastReportDto = dto;
            HasReport     = true;

            TrySummary = dto.CurrencySummaries.FirstOrDefault(c => c.Currency == CurrencyType.TRY)
                         ?? EmptySummary(CurrencyType.TRY, "TL");
            UsdSummary = dto.CurrencySummaries.FirstOrDefault(c => c.Currency == CurrencyType.USD)
                         ?? EmptySummary(CurrencyType.USD, "USD");
            EurSummary = dto.CurrencySummaries.FirstOrDefault(c => c.Currency == CurrencyType.EUR)
                         ?? EmptySummary(CurrencyType.EUR, "EUR");

            // İşlem türü tablosu — DTO'yu DataGrid için düzleştir
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

                TypeRows.Add(new TransactionTypeRow(
                    ts.TypeDisplay,
                    tryAmt, tryCnt,
                    usdAmt, usdCnt,
                    eurAmt, eurCnt));
            }
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

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// DataGrid bağlaması için düzleştirilmiş işlem türü satırı.
/// Para birimleri sabit 3 sütuna (TL / USD / EUR) ayrılmıştır.
/// </summary>
public record TransactionTypeRow(
    string  TypeDisplay,
    decimal TryAmount, int TryCount,
    decimal UsdAmount, int UsdCount,
    decimal EurAmount, int EurCount);
