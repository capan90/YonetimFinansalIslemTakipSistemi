using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCashTransactions;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;

public class CashTransactionListViewModel : INotifyPropertyChanged
{
    private readonly GetCashTransactionsHandler _handler;

    private DateTime? _dateFrom;
    private DateTime? _dateTo;
    private string?   _selectedTransactionType;
    private string?   _selectedCurrencyType;

    public CashTransactionListViewModel(GetCashTransactionsHandler handler)
    {
        _handler      = handler;
        FilterCommand = new RelayCommand(async () => await ExecuteFilterAsync());
    }

    // --- Filtre alanları ---

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

    /// <summary>
    /// ComboBox'taki Türkçe string seçimi. Null veya "Tümü" → filtre yok.
    /// </summary>
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

    // --- ComboBox kaynakları ---

    public IReadOnlyList<string> TransactionTypeOptions { get; } =
        new[] { "Tümü", "Tahsilat", "Ödeme", "Avans", "Özel Harcama", "Transfer" };

    public IReadOnlyList<string> CurrencyTypeOptions { get; } =
        new[] { "Tümü", "TRY", "USD", "EUR" };

    // --- DataGrid kaynağı ---

    public ObservableCollection<CashTransactionDto> Transactions { get; } = new();

    // --- Komutlar ---

    public ICommand FilterCommand { get; }

    /// <summary>İlk açılışta tüm kayıtları yükler.</summary>
    public async Task LoadAsync() => await ExecuteFilterAsync();

    private async Task ExecuteFilterAsync()
    {
        var query = new GetCashTransactionsQuery
        {
            DateFrom        = DateFrom,
            DateTo          = DateTo,
            TransactionType = ParseTransactionType(SelectedTransactionType),
            CurrencyType    = ParseCurrencyType(SelectedCurrencyType)
        };

        var results = await _handler.HandleAsync(query);

        Transactions.Clear();
        foreach (var item in results)
            Transactions.Add(item);
    }

    // "Tümü" veya null → null (filtre uygulanmaz)
    private static TransactionType? ParseTransactionType(string? display) => display switch
    {
        "Tahsilat"     => TransactionType.Tahsilat,
        "Ödeme"        => TransactionType.Odeme,
        "Avans"        => TransactionType.Avans,
        "Özel Harcama" => TransactionType.OzelHarcama,
        "Transfer"     => TransactionType.Transfer,
        _              => null
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
