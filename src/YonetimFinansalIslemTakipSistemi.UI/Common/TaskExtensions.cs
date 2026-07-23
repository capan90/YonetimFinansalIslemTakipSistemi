namespace YonetimFinansalIslemTakipSistemi.UI.Common;

/// <summary>
/// Fire-and-forget task'lar için güvenli tetikleme.
/// `_ = SomethingAsync()` deseninde hata sessizce kaybolur (ekran boş kalır, iz yok);
/// Forget() hatayı UI thread'ine taşır — App.OnDispatcherUnhandledException loglar ve
/// kullanıcıya bildirir, uygulama kapanmaz.
/// </summary>
public static class TaskExtensions
{
    public static void Forget(this Task task)
        => task.ContinueWith(
            t => System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                new Action(() => throw t.Exception!.GetBaseException())),
            TaskContinuationOptions.OnlyOnFaulted);
}
