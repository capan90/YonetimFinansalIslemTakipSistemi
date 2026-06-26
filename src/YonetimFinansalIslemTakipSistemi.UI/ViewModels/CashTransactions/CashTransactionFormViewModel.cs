using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.CreateCashTransaction;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.UpdateCashTransaction;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCashTransactions;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;

public class CashTransactionFormViewModel : INotifyPropertyChanged
{
    private readonly CreateCashTransactionHandler _createHandler;
    private readonly UpdateCashTransactionHandler _updateHandler;
    private readonly IUserContext _userContext;

    private Guid?    _editTargetId;
    private DateTime _transactionDate         = DateTime.Today;
    private string   _selectedTransactionType = "Giriş";
    private string   _selectedCurrencyType    = "TRY";
    private string   _amountText              = string.Empty;
    private string   _description             = string.Empty;
    private string?  _errorMessage;

    public bool IsEditMode { get; private set; }
    public bool IsCopyMode { get; private set; }

    public string WindowTitle => IsEditMode ? "İşlemi Düzenle"
                               : IsCopyMode ? "Yeni İşlem (Kopyadan)"
                               : "Yeni Nakit İşlem";

    public DateTime TransactionDate
    {
        get => _transactionDate;
        set { _transactionDate = value; OnPropertyChanged(); }
    }

    public IReadOnlyList<string> TransactionTypeOptions { get; } =
        new[] { "Giriş", "Çıkış" };

    public IReadOnlyList<string> CurrencyTypeOptions { get; } =
        new[] { "TRY", "USD", "EUR" };

    public string SelectedTransactionType
    {
        get => _selectedTransactionType;
        set { _selectedTransactionType = value; OnPropertyChanged(); }
    }

    public string SelectedCurrencyType
    {
        get => _selectedCurrencyType;
        set { _selectedCurrencyType = value; OnPropertyChanged(); }
    }

    public string AmountText
    {
        get => _amountText;
        set { _amountText = value; OnPropertyChanged(); }
    }

    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public Action? SaveCompleted { get; set; }

    public ICommand SaveCommand { get; }

    public CashTransactionFormViewModel(
        CreateCashTransactionHandler createHandler,
        UpdateCashTransactionHandler updateHandler,
        IUserContext userContext)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _userContext   = userContext;
        SaveCommand    = new RelayCommand(async () => await ExecuteSaveAsync());
    }

    /// <summary>
    /// Düzenleme modunda formu doldurur. ShowDialog() öncesi çağrılmalıdır.
    /// DTO'daki display string'ler form ComboBox seçenekleriyle birebir uyuşur (GetCashTransactionsHandler mapping'i).
    /// </summary>
    public void Initialize(CashTransactionDto dto)
    {
        IsEditMode              = true;
        _editTargetId           = dto.Id;
        TransactionDate         = dto.TransactionDate;
        SelectedTransactionType = dto.TransactionTypeDisplay;
        SelectedCurrencyType    = dto.CurrencyTypeDisplay;
        // Tutar: Borç veya Alacak, hangisi dolu ise onu göster
        var amount = dto.Borc > 0 ? dto.Borc : dto.Alacak;
        AmountText              = amount.ToString(CultureInfo.CurrentCulture);
        Description             = dto.Description;
        OnPropertyChanged(nameof(WindowTitle));
    }

    /// <summary>
    /// Kopyalama modunda formu doldurur — Id/audit alanları kopyalanmaz, form create modunda açılır.
    /// </summary>
    public void InitializeForCopy(CashTransactionDto dto)
    {
        // IsEditMode=false kalır → Save, CreateHandler'ı çağırır
        IsCopyMode              = true;
        TransactionDate         = dto.TransactionDate;
        SelectedTransactionType = dto.TransactionTypeDisplay;
        SelectedCurrencyType    = dto.CurrencyTypeDisplay;
        var amount = dto.Borc > 0 ? dto.Borc : dto.Alacak;
        AmountText              = amount.ToString(CultureInfo.CurrentCulture);
        Description             = dto.Description;
        OnPropertyChanged(nameof(WindowTitle));
    }

    private async Task ExecuteSaveAsync()
    {
        ErrorMessage = null;

        // Açıklama zorunlu — UI düzeyinde de kontrol
        if (string.IsNullOrWhiteSpace(Description))
        {
            ErrorMessage = "Açıklama alanı zorunludur. Lütfen işlem açıklaması giriniz.";
            return;
        }

        // Tutar parse — kullanıcı virgül veya nokta kullanabilir
        var normalized = AmountText?.Replace(',', '.') ?? string.Empty;
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            ErrorMessage = "Tutar geçerli bir sayı olmalıdır. (Örnek: 1250,50)";
            return;
        }

        if (IsEditMode)
        {
            var request = new UpdateCashTransactionRequest
            {
                Id              = _editTargetId!.Value,
                TransactionDate = TransactionDate,
                TransactionType = ParseTransactionType(SelectedTransactionType),
                CurrencyType    = ParseCurrencyType(SelectedCurrencyType),
                Amount          = amount,
                Description     = Description.Trim(),
                UpdatedByUserId = _userContext.UserId
            };
            var result = await _updateHandler.HandleAsync(request);
            if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }
        }
        else
        {
            var request = new CreateCashTransactionRequest
            {
                TransactionDate = TransactionDate,
                TransactionType = ParseTransactionType(SelectedTransactionType),
                CurrencyType    = ParseCurrencyType(SelectedCurrencyType),
                Amount          = amount,
                Description     = Description.Trim(),
                CreatedByUserId = _userContext.UserId
            };
            var result = await _createHandler.HandleAsync(request);
            if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }
        }

        SaveCompleted?.Invoke();
    }

    private static TransactionType ParseTransactionType(string display) => display switch
    {
        "Giriş" => TransactionType.Giris,
        "Çıkış" => TransactionType.Cikis,
        _       => TransactionType.Giris
    };

    private static CurrencyType ParseCurrencyType(string display) => display switch
    {
        "USD" => CurrencyType.USD,
        "EUR" => CurrencyType.EUR,
        _     => CurrencyType.TRY
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
