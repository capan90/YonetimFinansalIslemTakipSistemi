using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.ExchangeRates.Commands.CreateOrUpdateExchangeRate;
using YonetimFinansalIslemTakipSistemi.Application.Features.ExchangeRates.Queries.GetExchangeRates;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.ExchangeRates;

public class ExchangeRateViewModel : INotifyPropertyChanged
{
    private readonly CreateOrUpdateExchangeRateHandler _saveHandler;
    private readonly GetExchangeRatesHandler           _loadHandler;
    private readonly IDialogService                    _dialogService;

    private DateTime? _rateDate = DateTime.Today;
    private string    _selectedCurrencyDisplay = "USD";
    private string    _forexBuying             = string.Empty;
    private string    _forexSelling            = string.Empty;
    private string?   _formError;
    private DateTime? _filterDateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime? _filterDateTo   = DateTime.Today;
    private string    _filterCurrencyDisplay = "Tümü";
    private bool      _isLoading;

    public List<string> CurrencyOptions       { get; } = ["USD", "EUR"];
    public List<string> FilterCurrencyOptions { get; } = ["Tümü", "USD", "EUR"];

    public ObservableCollection<ExchangeRateDto> Rates { get; } = new();

    public DateTime? RateDate
    {
        get => _rateDate;
        set { _rateDate = value; OnPropertyChanged(); }
    }

    public string SelectedCurrencyDisplay
    {
        get => _selectedCurrencyDisplay;
        set { _selectedCurrencyDisplay = value; OnPropertyChanged(); }
    }

    public string ForexBuying
    {
        get => _forexBuying;
        set { _forexBuying = value; OnPropertyChanged(); }
    }

    public string ForexSelling
    {
        get => _forexSelling;
        set { _forexSelling = value; OnPropertyChanged(); }
    }

    public string? FormError
    {
        get => _formError;
        set { _formError = value; OnPropertyChanged(); }
    }

    public DateTime? FilterDateFrom
    {
        get => _filterDateFrom;
        set { _filterDateFrom = value; OnPropertyChanged(); }
    }

    public DateTime? FilterDateTo
    {
        get => _filterDateTo;
        set { _filterDateTo = value; OnPropertyChanged(); }
    }

    public string FilterCurrencyDisplay
    {
        get => _filterCurrencyDisplay;
        set { _filterCurrencyDisplay = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public ICommand SaveCommand      { get; }
    public ICommand LoadCommand      { get; }
    public ICommand ClearFormCommand { get; }

    public ExchangeRateViewModel(
        CreateOrUpdateExchangeRateHandler saveHandler,
        GetExchangeRatesHandler           loadHandler,
        IDialogService                    dialogService)
    {
        _saveHandler   = saveHandler;
        _loadHandler   = loadHandler;
        _dialogService = dialogService;

        SaveCommand      = new RelayCommand(async () => await SaveAsync());
        LoadCommand      = new RelayCommand(async () => await LoadAsync());
        ClearFormCommand = new RelayCommand(ClearForm);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        FormError = null;

        try
        {
            var result = await _loadHandler.HandleAsync(new GetExchangeRatesQuery
            {
                DateFrom     = FilterDateFrom,
                DateTo       = FilterDateTo,
                CurrencyType = ParseFilterCurrency()
            });

            Rates.Clear();
            if (result.Success)
                foreach (var r in result.Data!) Rates.Add(r);
            else
                FormError = result.ErrorMessage;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveAsync()
    {
        FormError = null;

        if (!TryParseDecimal(ForexBuying, out var buying) || buying <= 0)
        {
            FormError = "Alış kuru geçerli bir sayı olmalıdır.";
            return;
        }

        if (!TryParseDecimal(ForexSelling, out var selling) || selling <= 0)
        {
            FormError = "Satış kuru geçerli bir sayı olmalıdır.";
            return;
        }

        // Satış < Alış: iş kuralı belirsiz — kullanıcıya sor, blocker değil
        if (selling < buying)
        {
            if (!_dialogService.ShowConfirmation(
                    "Satış kuru alış kurundan küçük. Devam etmek istiyor musunuz?", "Uyarı"))
                return;
        }

        var result = await _saveHandler.HandleAsync(new CreateOrUpdateExchangeRateCommand
        {
            RateDate     = RateDate ?? DateTime.Today,
            CurrencyType = ParseFormCurrency(),
            ForexBuying  = buying,
            ForexSelling = selling
        });

        if (!result.Success)
        {
            FormError = result.ErrorMessage;
            return;
        }

        _dialogService.ShowSuccess("Başarılı", "Kur kaydedildi.");
        ClearForm();
        await LoadAsync();
    }

    private void ClearForm()
    {
        RateDate                = DateTime.Today;
        SelectedCurrencyDisplay = "USD";
        ForexBuying             = string.Empty;
        ForexSelling            = string.Empty;
        FormError               = null;
    }

    private CurrencyType ParseFormCurrency()
        => SelectedCurrencyDisplay == "EUR" ? CurrencyType.EUR : CurrencyType.USD;

    private CurrencyType? ParseFilterCurrency() => FilterCurrencyDisplay switch
    {
        "USD" => CurrencyType.USD,
        "EUR" => CurrencyType.EUR,
        _     => null
    };

    // Virgül ve nokta her ikisi de ondalık ayırıcı olarak kabul edilir
    private static bool TryParseDecimal(string input, out decimal value)
        => decimal.TryParse(input.Replace(',', '.'),
               NumberStyles.Any, CultureInfo.InvariantCulture, out value);

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
