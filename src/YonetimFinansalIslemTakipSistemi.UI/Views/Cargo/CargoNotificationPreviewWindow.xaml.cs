using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification.MarkCargoNotificationPrepared;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

public partial class CargoNotificationPreviewWindow : Window
{
    private readonly IServiceProvider       _services;
    private readonly IDialogService         _dialogService;
    private readonly CargoShipmentDirection _direction;
    private readonly NotificationType       _notificationType;
    private          CargoNotificationModel _model = null!;

    /// <summary>
    /// WhatsApp Web açıldı veya mail başarıyla gönderildiyse true.
    /// Operation Center bu değere göre bildirim durumunu günceller.
    /// </summary>
    public bool WasMarkedPrepared { get; private set; }

    public CargoNotificationPreviewWindow(
        IServiceProvider services,
        CargoShipmentDirection direction,
        NotificationType notificationType = NotificationType.WhatsApp)
    {
        InitializeComponent();
        _services         = services;
        _direction        = direction;
        _notificationType = notificationType;
        _dialogService    = services.GetRequiredService<IDialogService>();
    }

    /// <summary>Handler tarafından üretilen modeli ekrana yansıtır; mode'a göre alanları ayarlar.</summary>
    public void Initialize(CargoNotificationModel model)
    {
        _model = model;

        ShipmentNumberBlock.Text = model.ShipmentNumber ?? "—";
        ReceiverBlock.Text       = model.ReceiverCompany ?? "—";
        MessageBodyBox.Text      = model.MessageBody;

        if (_notificationType == NotificationType.Mail)
            InitializeMailMode(model);
        else
            InitializeWhatsAppMode(model);
    }

    private void InitializeMailMode(CargoNotificationModel model)
    {
        Title           = "Mail Hazırla";
        TitleBlock.Text = "✉ Mail Hazırla";

        // Gönderici adresi: CargoNotificationOptions → SmtpNotificationOptions.From → SmtpUsername
        var cargoOpts = _services.GetRequiredService<CargoNotificationOptions>();
        var smtpOpts  = _services.GetRequiredService<SmtpNotificationOptions>();
        var fromEmail  = !string.IsNullOrWhiteSpace(cargoOpts.FromEmail) ? cargoOpts.FromEmail
                        : !string.IsNullOrWhiteSpace(smtpOpts.From)       ? smtpOpts.From
                        : smtpOpts.SmtpUsername;

        FromBlock.Text      = fromEmail;
        ToTextBox.Text      = model.TargetEmail ?? string.Empty;
        SubjectTextBox.Text = model.Subject ?? $"Kargo Bilgilendirme - {model.ShipmentNumber}";

        MailFieldsPanel.Visibility = Visibility.Visible;
        MailSendButton.Visibility  = Visibility.Visible;

        // Alıcı alanı boşsa gönder butonu devre dışı; kullanıcı manuel doldurabilir
        MailSendButton.IsEnabled = !string.IsNullOrWhiteSpace(ToTextBox.Text);
    }

    private void InitializeWhatsAppMode(CargoNotificationModel model)
    {
        Title           = "WhatsApp Mesajı Hazırla";
        TitleBlock.Text = "💬 WhatsApp Mesajı Hazırla";

        PhoneLabelBlock.Visibility = Visibility.Visible;
        PhoneValueBlock.Visibility = Visibility.Visible;
        PhoneValueBlock.Text = !string.IsNullOrWhiteSpace(model.TargetPhone)
            ? model.TargetPhone : "Telefon bilgisi girilmemiş";

        WhatsAppWebButton.Visibility = Visibility.Visible;
    }

    // ── Kopyala ──────────────────────────────────────────────────────────

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_model?.MessageBody)) return;
        Clipboard.SetText(_model.MessageBody);
        _dialogService.ShowInfo("Mesaj panoya kopyalandı.", "Kopyalandı");
    }

    // ── WhatsApp Web ──────────────────────────────────────────────────────

    private async void WhatsAppWebButton_Click(object sender, RoutedEventArgs e)
    {
        var phone   = NormalizePhone(_model?.TargetPhone);
        var encoded = Uri.EscapeDataString(_model?.MessageBody ?? string.Empty);

        var url = string.IsNullOrWhiteSpace(phone)
            ? $"https://wa.me/?text={encoded}"
            : $"https://wa.me/{phone}?text={encoded}";

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"WhatsApp Web açılamadı: {ex.Message}");
            return;
        }

        // Tarayıcı başarıyla açıldı → bildirim durumunu otomatik güncelle
        WhatsAppWebButton.IsEnabled = false;
        await MarkPreparedAsync();
        await ShowSuccessAndCloseAsync("WhatsApp Web açıldı. Bildirim durumu güncellendi.");
    }

    // ── Mail Gönder ───────────────────────────────────────────────────────

    private async void MailSendButton_Click(object sender, RoutedEventArgs e)
    {
        var to = ToTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(to))
        {
            _dialogService.ShowWarning("Alıcı e-posta adresi girilmemiş.", "Mail Gönderilemedi");
            return;
        }

        MailSendButton.IsEnabled = false;

        var mailSender       = _services.GetRequiredService<ICargoMailSenderService>();
        var cc               = string.IsNullOrWhiteSpace(CcTextBox.Text) ? null : CcTextBox.Text.Trim();
        var subject          = SubjectTextBox.Text.Trim();
        var (success, error) = await mailSender.SendAsync(to, cc, subject, _model.MessageBody);

        if (!success)
        {
            MailSendButton.IsEnabled = true;
            // Teknik detay log dosyasına yazılır; kullanıcıya kısa ve anlaşılır mesaj gösterilir
            _dialogService.ShowError(BuildMailErrorMessage(error), "Mail Gönderilemedi");
            return;
        }

        // Başarılı gönderim → bildirim durumunu otomatik güncelle, başarı bildirimi göster
        await MarkPreparedAsync();
        await ShowSuccessAndCloseAsync("Mail başarıyla gönderildi. Bildirim durumu güncellendi.");
    }

    // ── Ortak: durumu güncelle ────────────────────────────────────────────

    private async Task MarkPreparedAsync()
    {
        var handler = _services.GetRequiredService<MarkCargoNotificationPreparedHandler>();
        var result  = await handler.HandleAsync(new MarkCargoNotificationPreparedRequest
        {
            CargoShipmentId  = _model.ShipmentId,
            Direction        = _direction,
            NotificationType = _notificationType
        });

        if (!result.Success)
        {
            _dialogService.ShowError(result.ErrorMessage ?? "Bildirim durumu güncellenemedi.");
            return;
        }

        WasMarkedPrepared = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    // ── Başarı bildirimi ──────────────────────────────────────────────────

    /// <summary>
    /// Yeşil başarı alanını gösterir, 1.5 saniye bekler, sonra pencereyi kapatır.
    /// Kullanıcı işlemin tamamlandığını görmeden pencere kaybolmaz.
    /// </summary>
    private async Task ShowSuccessAndCloseAsync(string message)
    {
        SuccessToastText.Text   = message;
        SuccessToast.Visibility = Visibility.Visible;
        await Task.Delay(1500);
        Close();
    }

    // ── Kullanıcı dostu SMTP hata mesajı ─────────────────────────────────

    private static string BuildMailErrorMessage(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return "Mail gönderilemedi. Beklenmeyen bir hata oluştu.";

        // Gönderici adresi yetkisi yoksa (SendAsDenied / not allowed to send as)
        if (error.Contains("SendAs", StringComparison.OrdinalIgnoreCase)
            || error.Contains("send as", StringComparison.OrdinalIgnoreCase)
            || error.Contains("not allowed to send", StringComparison.OrdinalIgnoreCase))
            return "Mail gönderilemedi.\n\n" +
                   "Gönderici adres, SMTP hesabı adına mail gönderme yetkisine sahip değil.\n" +
                   "appsettings.json → CargoNotifications:FromEmail değerini SMTP kullanıcı adresiyle aynı yapın\n" +
                   "veya Exchange/Outlook tarafında 'Send As' yetkisi verin.";

        // Kimlik doğrulama hatası
        if (error.Contains("535", StringComparison.OrdinalIgnoreCase)
            || error.Contains("auth", StringComparison.OrdinalIgnoreCase)
            || error.Contains("credentials", StringComparison.OrdinalIgnoreCase))
            return "Mail gönderilemedi.\n\nSMTP kimlik doğrulama hatası. Kullanıcı adı veya şifre yanlış olabilir.";

        // Bağlantı / zaman aşımı
        if (error.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || error.Contains("SocketException", StringComparison.OrdinalIgnoreCase)
            || error.Contains("ConnectFailure", StringComparison.OrdinalIgnoreCase))
            return "Mail gönderilemedi.\n\nSMTP sunucusuna bağlanılamadı. İnternet bağlantısını veya SMTP sunucu adresini kontrol edin.";

        return $"Mail gönderilemedi.\n\n{error}";
    }

    // ── Telefon normalleştirme ─────────────────────────────────────────────

    /// <summary>
    /// wa.me URL'i için uluslararası format: 0532..., +90 532..., 00905..., (0532)1234567 → 905321234567
    /// </summary>
    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digits)) return null;

        // 00 ile başlıyorsa (00905...) → çift sıfır öncekini kaldır
        if (digits.StartsWith("00"))
            digits = digits[2..];

        // Zaten +90 / 90 formatında
        if (digits.StartsWith("90"))
            return digits;

        // Yerel Türkiye (0532...) → 90 + yerel
        if (digits.StartsWith("0"))
            return "90" + digits[1..];

        // Sade 10 haneli (5321234567) → 90 ekle
        return "90" + digits;
    }
}
