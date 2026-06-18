using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Commands.CreateCashTransaction;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.CashTransactions;

public class CashTransactionFormViewModel : INotifyPropertyChanged
{
    private readonly CreateCashTransactionHandler _handler;
    private readonly IUserContext _userContext;

    private DateTime _transactionDate = DateTime.Today;
    private string   _selectedTransactionType = "Tahsilat";
    private string   _selectedCurrencyType    = "TRY";
    private string   _amountText              = string.Empty;
    private string   _description             = string.Empty;
    private string?  _errorMessage;

    public DateTime TransactionDate
    {
        get => _transactionDate;
        set { _transactionDate = value; OnPropertyChanged(); }
    }

    public IReadOnlyList<string> TransactionTypeOptions { get; } =
        new[] { "Tahsilat", "Ödeme", "Avans", "Özel Harcama", "Transfer" };

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

    public CashTransactionFormViewModel(CreateCashTransactionHandler handler, IUserContext userContext)
    {
        _handler     = handler;
        _userContext = userContext;
        SaveCommand  = new RelayCommand(async () => await ExecuteSaveAsync());
    }

    private async Task ExecuteSaveAsync()
    {
        ErrorMessage = null;

        // Tutar parse — kullanıcı virgül veya nokta kullanabilir
        var normalized = AmountText?.Replace(',', '.') ?? string.Empty;
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            ErrorMessage = "Tutar geçerli bir sayı olmalıdır. (Örnek: 1250,50)";
            return;
        }

        var request = new CreateCashTransactionRequest
        {
            TransactionDate = TransactionDate,
            TransactionType = ParseTransactionType(SelectedTransactionType),
            CurrencyType    = ParseCurrencyType(SelectedCurrencyType),
            Amount          = amount,
            Description     = Description.Trim(), // opsiyonel; girilmişse boşluklar temizlenir
            CreatedByUserId = _userContext.UserId
        };

        var result = await _handler.HandleAsync(request);
        if (!result.Success) { ErrorMessage = result.ErrorMessage; return; }

        SaveCompleted?.Invoke();
    }

    private static TransactionType ParseTransactionType(string display) => display switch
    {
        "Tahsilat"     => TransactionType.Tahsilat,
        "Ödeme"        => TransactionType.Odeme,
        "Avans"        => TransactionType.Avans,
        "Özel Harcama" => TransactionType.OzelHarcama,
        "Transfer"     => TransactionType.Transfer,
        _              => TransactionType.Tahsilat
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
