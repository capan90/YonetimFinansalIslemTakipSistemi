using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Media;
using YonetimFinansalIslemTakipSistemi.Application.Features.Settings.MailSettings;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Settings;

public partial class MailSettingsWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly IDialogService   _dialogService;

    public MailSettingsWindow(IServiceProvider services)
    {
        InitializeComponent();
        _services      = services;
        _dialogService = services.GetRequiredService<IDialogService>();
        Loaded += async (_, _) => await LoadCurrentSettingsAsync();
    }

    private async Task LoadCurrentSettingsAsync()
    {
        var handler = _services.GetRequiredService<GetMailSettingsHandler>();
        var result  = await handler.HandleAsync();

        if (!result.Success || result.Data is null) return;

        var dto = result.Data;
        SmtpHostBox.Text         = dto.SmtpHost;
        SmtpPortBox.Text         = dto.SmtpPort.ToString();
        EnableSslCheck.IsChecked = dto.EnableSsl;
        SenderEmailBox.Text      = dto.SenderEmail;
        SenderNameBox.Text       = dto.SenderName;
        UsernameBox.Text         = dto.Username;
        // Şifre kasıtlı boş — mevcut şifre korunur, ekranda gösterilmez
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var dto = BuildDto();
        if (dto is null) return;

        var handler = _services.GetRequiredService<SaveMailSettingsHandler>();
        var result  = await handler.HandleAsync(dto);

        if (result.Success)
            _dialogService.ShowSuccess("Mail ayarları kaydedildi.", "Kayıt Başarılı");
        else
            _dialogService.ShowError(result.ErrorMessage ?? "Kayıt başarısız.", "Hata");
    }

    private async void SendTestButton_Click(object sender, RoutedEventArgs e)
    {
        TestResultBlock.Visibility = Visibility.Collapsed;

        var dto = BuildDto();
        if (dto is null) return;

        var recipient = TestRecipientBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            _dialogService.ShowWarning("Test alıcısı giriniz.", "Test Maili");
            return;
        }

        var handler = _services.GetRequiredService<SendTestMailHandler>();
        var result  = await handler.HandleAsync(dto, recipient);

        TestResultBlock.Visibility = Visibility.Visible;

        if (result.Success)
        {
            TestResultBlock.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
            TestResultBlock.Text       = $"Test maili başarıyla gönderildi: {recipient}";
        }
        else
        {
            TestResultBlock.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
            TestResultBlock.Text       = result.ErrorMessage ?? "Gönderilemedi.";
        }
    }

    private MailSettingsDto? BuildDto()
    {
        if (string.IsNullOrWhiteSpace(SmtpHostBox.Text))
        {
            _dialogService.ShowWarning("SMTP sunucusu zorunludur.", "Doğrulama");
            return null;
        }

        if (!int.TryParse(SmtpPortBox.Text, out var port) || port is < 1 or > 65535)
        {
            _dialogService.ShowWarning("Geçerli bir port numarası giriniz (1–65535).", "Doğrulama");
            return null;
        }

        return new MailSettingsDto
        {
            SmtpHost    = SmtpHostBox.Text.Trim(),
            SmtpPort    = port,
            EnableSsl   = EnableSslCheck.IsChecked == true,
            SenderEmail = SenderEmailBox.Text.Trim(),
            SenderName  = SenderNameBox.Text.Trim(),
            Username    = UsernameBox.Text.Trim(),
            Password    = PasswordBox.Password, // boş ise handler mevcut şifreyi korur
        };
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
