using System.Windows.Input;

namespace YonetimFinansalIslemTakipSistemi.UI.Common;

/// <summary>
/// Harici MVVM paketi olmadan ICommand implementasyonu.
/// Async kullanım: çalışma bitene kadar komut yeniden tetiklenemez ve bağlı
/// buton devre dışı kalır — çift tıklamayla mükerrer kayıt (finans/kargo) önlenir.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action? _execute;
    private readonly Func<Task>? _executeAsync;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute    = execute;
        _canExecute = canExecute;
    }

    /// <summary>
    /// Async komut. `new RelayCommand(async () => await ...)` çağrıları derleyici
    /// tarafından otomatik bu overload'a bağlanır (async lambda → Func&lt;Task&gt; tercih edilir).
    /// </summary>
    public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute   = canExecute;
    }

    public bool CanExecute(object? parameter)
        => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (_executeAsync is null)
        {
            _execute!();
            return;
        }

        // Re-entrancy koruması: işlem sürerken ikinci tetikleme yok sayılır
        if (_isExecuting) return;
        _isExecuting = true;
        CommandManager.InvalidateRequerySuggested();
        try
        {
            // Hata durumunda istisna Dispatcher'a düşer; App.OnDispatcherUnhandledException
            // loglayıp kullanıcıya bildirir (önceki davranışla aynı)
            await _executeAsync();
        }
        finally
        {
            _isExecuting = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add    => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
