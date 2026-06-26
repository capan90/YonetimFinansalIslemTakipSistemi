using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification.MarkCargoNotificationPrepared;
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
    /// Kullanıcı "Hazırlandı Olarak İşaretle" butonuna bastıysa true.
    /// Liste ekranı bu değere göre yenileme kararı verir.
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

    /// <summary>Handler sonucundaki modeli ekrana yansıtır.</summary>
    public void Initialize(CargoNotificationModel model)
    {
        _model = model;

        ShipmentNumberBlock.Text = model.ShipmentNumber ?? "—";
        ReceiverBlock.Text       = model.ReceiverCompany ?? "—";
        MessageBodyBox.Text      = model.MessageBody;

        if (_notificationType == NotificationType.Mail)
        {
            Title                    = "Mail Hazırla";
            TitleBlock.Text          = "Mail Hazırla";

            // Satır 1: Kime (e-posta)
            Row1LabelBlock.Text      = "Kime:";
            Row1ValueBlock.Text      = string.IsNullOrWhiteSpace(model.TargetEmail)
                ? "E-posta bilgisi girilmemiş"
                : model.TargetEmail;

            // Satır 2: Konu
            SubjectLabelBlock.Visibility = Visibility.Visible;
            SubjectBlock.Visibility      = Visibility.Visible;
            SubjectBlock.Text            = model.Subject ?? "—";

            // Butonlar: WhatsApp gizli, Mail Gönder görünür (disabled)
            WhatsAppWebButton.Visibility = Visibility.Collapsed;
            MailSendButton.Visibility    = Visibility.Visible;

            MarkPreparedButton.ToolTip   = "Bildirim durumunu 'Mail Hazır' yapar ve audit kaydı oluşturur";
        }
        else
        {
            Title                    = "WhatsApp Mesajı Hazırla";
            TitleBlock.Text          = "WhatsApp Mesajı Hazırla";

            // Satır 1: Telefon
            Row1LabelBlock.Text      = "Telefon:";
            Row1ValueBlock.Text      = string.IsNullOrWhiteSpace(model.TargetPhone)
                ? "Telefon bilgisi girilmemiş"
                : model.TargetPhone;

            // Butonlar: WhatsApp görünür, Mail Gönder gizli
            WhatsAppWebButton.Visibility = Visibility.Visible;
            MailSendButton.Visibility    = Visibility.Collapsed;

            MarkPreparedButton.ToolTip   = "Bildirim durumunu 'WhatsApp Hazır' yapar ve audit kaydı oluşturur";
        }
    }

    // ── Kopyala ──────────────────────────────────────────────────────────

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_model?.MessageBody)) return;
        Clipboard.SetText(_model.MessageBody);
        _dialogService.ShowInfo("Mesaj panoya kopyalandı.", "Kopyalandı");
    }

    // ── WhatsApp Web ──────────────────────────────────────────────────────

    private void WhatsAppWebButton_Click(object sender, RoutedEventArgs e)
    {
        var encodedMessage = Uri.EscapeDataString(_model?.MessageBody ?? string.Empty);
        var phone          = NormalizePhone(_model?.TargetPhone);

        var url = string.IsNullOrWhiteSpace(phone)
            ? $"https://wa.me/?text={encodedMessage}"
            : $"https://wa.me/{phone}?text={encodedMessage}";

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"WhatsApp Web açılamadı: {ex.Message}");
        }
    }

    // ── Hazırlandı Olarak İşaretle ────────────────────────────────────────

    private async void MarkPreparedButton_Click(object sender, RoutedEventArgs e)
    {
        var handler = _services.GetRequiredService<MarkCargoNotificationPreparedHandler>();

        var result = await handler.HandleAsync(new MarkCargoNotificationPreparedRequest
        {
            CargoShipmentId  = _model.ShipmentId,
            Direction        = _direction,
            NotificationType = _notificationType
        });

        if (!result.Success)
        {
            _dialogService.ShowError(result.ErrorMessage ?? "Beklenmeyen bir hata oluştu.");
            return;
        }

        WasMarkedPrepared            = true;
        MarkPreparedButton.IsEnabled = false; // çift işaretlemeyi önle

        var durum = _notificationType == NotificationType.Mail ? "Mail Hazır" : "WhatsApp Hazır";
        _dialogService.ShowSuccess(
            $"'{_model.ShipmentNumber}' için bildirim durumu '{durum}' olarak güncellendi.",
            "Hazırlandı");
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();

    // ── Telefon normalleştirme ─────────────────────────────────────────────

    /// <summary>
    /// wa.me URL'i için sadece rakam içeren uluslararası format döner.
    /// Türkiye numarası: 0532... → 90532..., 532... → 90532...
    /// </summary>
    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digits)) return null;

        if (digits.Length == 11 && digits.StartsWith("0"))
            return "90" + digits[1..];

        if (digits.Length == 10)
            return "90" + digits;

        return digits;
    }
}
