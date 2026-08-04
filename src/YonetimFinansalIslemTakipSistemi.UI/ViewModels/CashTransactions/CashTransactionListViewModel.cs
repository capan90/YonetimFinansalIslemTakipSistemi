using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCashTransactions;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCurrentBalances;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;

public class CashTransactionListViewModel : INotifyPropertyChanged
{
    private readonly GetCashTransactionsHandler  _handler;
    private readonly GetCurrentBalancesHandler   _balanceHandler;

    private DateTime?           _dateFrom;
    private DateTime?           _dateTo;
    private string?             _selectedTransactionType;
    private string?             _selectedCurrencyType = CurrencyType.USD.ToString(); // Varsayılan: USD işlemleri göster
    private string?             _selectedAmountOperator;
    private string?             _amountValueText;
    private string?             _descriptionFilter;
    private CashTransactionDto? _selectedTransaction;

    // Bakiye kolonu görünürlük bayrakları — para birimi filtresine göre güncellenir
    private bool _showTlBalance  = true;
    private bool _showUsdBalance = false;
    private bool _showEurBalance = false;

    // Üst bakiye barı
    private decimal _tlBalance;
    private decimal _usdBalance;
    private decimal _eurBalance;

    public CashTransactionListViewModel(GetCashTransactionsHandler handler, GetCurrentBalancesHandler balanceHandler)
    {
        _handler        = handler;
        _balanceHandler = balanceHandler;
        FilterCommand   = new RelayCommand(async () => await LoadTransactionsAsync());
        UpdateBalanceColumnVisibility();
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
        set
        {
            _selectedCurrencyType = value;
            OnPropertyChanged();
            UpdateBalanceColumnVisibility();
        }
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

    public string? DescriptionFilter
    {
        get => _descriptionFilter;
        set { _descriptionFilter = value; OnPropertyChanged(); }
    }

    // --- Genel bakiye özeti (filtreden bağımsız, tüm zamanlar) ---

    public decimal TlBalance
    {
        get => _tlBalance;
        private set { _tlBalance = value; OnPropertyChanged(); }
    }

    public decimal UsdBalance
    {
        get => _usdBalance;
        private set { _usdBalance = value; OnPropertyChanged(); }
    }

    public decimal EurBalance
    {
        get => _eurBalance;
        private set { _eurBalance = value; OnPropertyChanged(); }
    }

    // --- Bakiye kolonu görünürlük özellikleri ---

    /// <summary>TRY seçiliyken veya Tümü seçiliyken true.</summary>
    public bool ShowTlBalance
    {
        get => _showTlBalance;
        private set { _showTlBalance = value; OnPropertyChanged(); }
    }

    /// <summary>USD seçiliyken veya Tümü seçiliyken true.</summary>
    public bool ShowUsdBalance
    {
        get => _showUsdBalance;
        private set { _showUsdBalance = value; OnPropertyChanged(); }
    }

    /// <summary>EUR seçiliyken veya Tümü seçiliyken true.</summary>
    public bool ShowEurBalance
    {
        get => _showEurBalance;
        private set { _showEurBalance = value; OnPropertyChanged(); }
    }

    // --- ComboBox kaynakları ---

    public IReadOnlyList<string> TransactionTypeOptions { get; } =
        new[] { "Tümü", "Giriş", "Çıkış" };

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

    // --- Liste özeti (salt okunur) ---
    // Filtre uygulandıktan SONRAKİ görünen satırların özeti. Üstteki bakiye
    // kartlarından farklıdır: kartlar filtreden bağımsız genel bakiyeyi gösterir,
    // bu şerit "şu an ekranda ne var" sorusunu yanıtlar.
    // Para birimleri karışık olabildiği için toplamlar birim yazılmadan verilir.

    public int VisibleCount => Transactions.Count;

    public decimal VisibleBorcTotal   => Transactions.Sum(t => t.Borc);
    public decimal VisibleAlacakTotal => Transactions.Sum(t => t.Alacak);

    public string ListSummary =>
        $"Gösterilen: {VisibleCount} kayıt   ·   Borç toplamı: {VisibleBorcTotal:N2}   ·   Alacak toplamı: {VisibleAlacakTotal:N2}";

    private void NotifySummaryChanged()
    {
        OnPropertyChanged(nameof(VisibleCount));
        OnPropertyChanged(nameof(VisibleBorcTotal));
        OnPropertyChanged(nameof(VisibleAlacakTotal));
        OnPropertyChanged(nameof(ListSummary));
    }

    // --- Komutlar ---

    public ICommand FilterCommand { get; }

    /// <summary>İşlem listesini ve genel bakiyeleri yükler (bakiye filtreden bağımsızdır).</summary>
    public async Task LoadTransactionsAsync()
    {
        await ExecuteFilterAsync();
        var balances = await _balanceHandler.HandleAsync();
        TlBalance  = balances.TlBalance;
        UsdBalance = balances.UsdBalance;
        EurBalance = balances.EurBalance;

        // ObservableCollection güncellenirken UI binding kesin tetiklensin.
        OnPropertyChanged(nameof(Transactions));

        // Eğer ICollectionView kullanılıyorsa Refresh çağrılsın.
        var view = CollectionViewSource.GetDefaultView(Transactions);
        if (view is not null)
        {
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(nameof(CashTransactionDto.TransactionDate), ListSortDirection.Descending));
            view.SortDescriptions.Add(new SortDescription(nameof(CashTransactionDto.CreatedAt), ListSortDirection.Descending));
            view.SortDescriptions.Add(new SortDescription(nameof(CashTransactionDto.Id), ListSortDirection.Descending));
            view.Refresh();
        }
    }

    [Obsolete("Use LoadTransactionsAsync instead")]
    public Task LoadAsync() => LoadTransactionsAsync();

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
            AmountOperator      = !string.IsNullOrEmpty(SelectedAmountOperator) && amountValue.HasValue
                                      ? SelectedAmountOperator
                                      : null,
            AmountValue         = amountValue,
            DescriptionContains = string.IsNullOrWhiteSpace(DescriptionFilter) ? null : DescriptionFilter.Trim()
        };

        var results = await _handler.HandleAsync(query);

        Transactions.Clear();
        foreach (var item in results)
            Transactions.Add(item);

        OnPropertyChanged(nameof(Transactions));
        NotifySummaryChanged();
    }

    private void UpdateBalanceColumnVisibility()
    {
        // Tümü veya null → tüm bakiye kolonları görünür
        var isTumu = string.IsNullOrEmpty(_selectedCurrencyType) || _selectedCurrencyType == "Tümü";
        ShowTlBalance  = isTumu || _selectedCurrencyType == CurrencyType.TRY.ToString();
        ShowUsdBalance = isTumu || _selectedCurrencyType == CurrencyType.USD.ToString();
        ShowEurBalance = isTumu || _selectedCurrencyType == CurrencyType.EUR.ToString();
    }

    // "Tümü" veya null → null (filtre uygulanmaz)
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
