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

    private DateTime?           _dateFrom;
    private DateTime?           _dateTo;
    private string?             _selectedTransactionType;
    private string?             _selectedCurrencyType;
    private string?             _selectedAmountOperator;
    private string?             _amountValueText;
    private CashTransactionDto? _selectedTransaction;

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

    // Operatör seçimi; null veya boş → tutar filtresi uygulanmaz
    public string? SelectedAmountOperator
    {
        get => _selectedAmountOperator;
        set { _selectedAmountOperator = value; OnPropertyChanged(); }
    }

    // Karşılaştırılacak tutar; geçersiz veya boş → filtre uygulanmaz
    public string? AmountValueText
    {
        get => _amountValueText;
        set { _amountValueText = value; OnPropertyChanged(); }
    }

    // --- ComboBox kaynakları ---

    public IReadOnlyList<string> TransactionTypeOptions { get; } =
        new[] { "Tümü", "Tahsilat", "Ödeme", "Avans", "Özel Harcama", "Transfer" };

    public IReadOnlyList<string> CurrencyTypeOptions { get; } =
        new[] { "Tümü", "TRY", "USD", "EUR" };

    // Boş string → "filtre yok" seçeneği; geri kalanlar karşılaştırma operatörleri
    public IReadOnlyList<string> AmountOperatorOptions { get; } =
        new[] { "", ">", ">=", "<", "<=", "=", "!=" };

    // --- Seçim (Düzenle / Sil butonlarını aktif eder) ---

    public CashTransactionDto? SelectedTransaction
    {
        get => _selectedTransaction;
        set
        {
            _selectedTransaction = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedTransaction));
        }
    }

    /// <summary>Toolbar butonlarının IsEnabled binding'i için.</summary>
    public bool HasSelectedTransaction => _selectedTransaction is not null;

    // --- DataGrid kaynağı ---

    public ObservableCollection<CashTransactionDto> Transactions { get; } = new();

    // --- Komutlar ---

    public ICommand FilterCommand { get; }

    /// <summary>İlk açılışta tüm kayıtları yükler.</summary>
    public async Task LoadAsync() => await ExecuteFilterAsync();

    private async Task ExecuteFilterAsync()
    {
        var amountValue = ParseAmount(AmountValueText);
        var query = new GetCashTransactionsQuery
        {
            DateFrom        = DateFrom,
            DateTo          = DateTo,
            TransactionType = ParseTransactionType(SelectedTransactionType),
            CurrencyType    = ParseCurrencyType(SelectedCurrencyType),
            // Operatör ve tutar ikisi birlikte dolu olduğunda filtre aktif olur
            AmountOperator  = !string.IsNullOrEmpty(SelectedAmountOperator) && amountValue.HasValue
                                  ? SelectedAmountOperator
                                  : null,
            AmountValue     = amountValue
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

    // Boş veya geçersiz metin → null (filtre uygulanmaz); hem nokta hem virgül ondalık ayıracı olarak kabul edilir
    private static decimal? ParseAmount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var normalized = text.Trim().Replace(',', '.');
        return decimal.TryParse(normalized,
                   System.Globalization.NumberStyles.Number,
                   System.Globalization.CultureInfo.InvariantCulture,
                   out var value)
            ? value
            : null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
