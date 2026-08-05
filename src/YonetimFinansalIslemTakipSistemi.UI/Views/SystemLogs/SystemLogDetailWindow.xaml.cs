using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.Application.Features.SystemLogs;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Common;

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
        CategoryText.Text      = d.Category;
        SourceText.Text        = d.Source ?? "—";
        UsernameText.Text      = d.Username ?? "—";
        MachineText.Text       = d.MachineName;
        VersionText.Text       = d.AppVersion ?? "—";
        StatusText.Text        = d.IsResolved ? "Çözüldü" : "Açık";

        // Renkler token'a DİNAMİK bağlanır: pencere açıkken tema değişirse
        // sabit fırça atansaydı eski renkte kalırlardı.
        ThemeBrush.Apply(LevelText,  TextBlock.ForegroundProperty, LevelTokenOf(d.Level));
        ThemeBrush.Apply(StatusText, TextBlock.ForegroundProperty,
            d.IsResolved ? "Theme.Success" : "Theme.Warning");

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

    // Seviye → tema token anahtarı. Bu metin pencere YÜZEYİ üzerinde durur
    // (rozet dolgusu üzerinde değil); bu yüzden *.Text rolleri kullanılır.
    private static string LevelTokenOf(SystemLogLevel level) => level switch
    {
        SystemLogLevel.Info     => "Theme.Info.Text",
        SystemLogLevel.Warning  => "Theme.Warning.Text",
        SystemLogLevel.Error    => "Theme.Danger.Text",
        SystemLogLevel.Critical => "Theme.Danger",
        _                       => "Theme.Text"
    };
}
