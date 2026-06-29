using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Features.SystemLogs;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.SystemLogs;

public partial class SystemLogDetailWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly Guid             _logId;
    private SystemLogDetailDto?       _detail;

    public SystemLogDetailWindow(IServiceProvider services, Guid logId)
    {
        _services = services;
        _logId    = logId;
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var svc = _services.GetRequiredService<ISystemLogService>();
        _detail = await svc.GetByIdAsync(_logId);
        if (_detail is null) { Close(); return; }

        BindDetail(_detail);
    }

    private void BindDetail(SystemLogDetailDto d)
    {
        CreatedAtText.Text     = d.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
        LevelText.Text         = d.LevelDisplay;
        LevelText.Foreground   = LevelToForeground(d.Level);
        CategoryText.Text      = d.Category;
        SourceText.Text        = d.Source ?? "—";
        UsernameText.Text      = d.Username ?? "—";
        MachineText.Text       = d.MachineName;
        VersionText.Text       = d.AppVersion ?? "—";
        StatusText.Text        = d.IsResolved ? "Çözüldü" : "Açık";
        StatusText.Foreground  = d.IsResolved
            ? new SolidColorBrush(Color.FromRgb(22, 163, 74))
            : new SolidColorBrush(Color.FromRgb(234, 88, 12));

        MessageText.Text = d.Message;

        var hasException = !string.IsNullOrWhiteSpace(d.ExceptionType);
        ExceptionTypeLabel.Visibility = hasException ? Visibility.Visible : Visibility.Collapsed;
        ExceptionTypeText.Visibility  = hasException ? Visibility.Visible : Visibility.Collapsed;
        ExceptionTypeText.Text        = d.ExceptionType ?? string.Empty;

        var hasInner = !string.IsNullOrWhiteSpace(d.InnerExceptionMessage);
        InnerExLabel.Visibility = hasInner ? Visibility.Visible : Visibility.Collapsed;
        InnerExText.Visibility  = hasInner ? Visibility.Visible : Visibility.Collapsed;
        InnerExText.Text        = d.InnerExceptionMessage ?? string.Empty;

        StackTraceText.Text = d.StackTrace ?? string.Empty;

        // Çözüm notu — eğer zaten çözüldüyse readonly göster
        if (d.IsResolved)
        {
            ResolutionNoteBox.Text       = d.ResolutionNote ?? string.Empty;
            ResolutionNoteBox.IsReadOnly = true;
            ResolveButton.IsEnabled      = false;
        }
    }

    private void CopyStackTrace_Click(object sender, RoutedEventArgs e)
    {
        var text = StackTraceText.Text;
        if (!string.IsNullOrWhiteSpace(text))
            Clipboard.SetText(text);
    }

    private async void MarkResolved_Click(object sender, RoutedEventArgs e)
    {
        if (_detail is null) return;

        var userContext = _services.GetRequiredService<IUserContext>();
        var svc         = _services.GetRequiredService<ISystemLogService>();

        await svc.MarkResolvedAsync(_logId, userContext.UserId, ResolutionNoteBox.Text.Trim());

        DialogResult = true; // listeyi yenilemesi için caller'a sinyal
    }

    private static Brush LevelToForeground(SystemLogLevel level) => level switch
    {
        SystemLogLevel.Info     => new SolidColorBrush(Color.FromRgb(29,  78,  216)),
        SystemLogLevel.Warning  => new SolidColorBrush(Color.FromRgb(146, 64,  14)),
        SystemLogLevel.Error    => new SolidColorBrush(Color.FromRgb(153, 27,  27)),
        SystemLogLevel.Critical => new SolidColorBrush(Color.FromRgb(220, 38,  38)),
        _                       => Brushes.Black
    };
}
