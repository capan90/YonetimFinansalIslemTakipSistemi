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
    private readonly IServiceProvider         _services;
    private readonly IDialogService           _dialogService;
    private readonly CargoShipmentDirection   _direction;
    private          CargoNotificationModel   _model = null!;

    /// <summary>
    /// Kullanıcı "Hazırlandı Olarak İşaretle" butonuna bastıysa true.
    /// Liste ekranı bu değere göre yenileme kararı verir.
    /// </summary>
    public bool WasMarkedPrepared { get; private set; }

    public CargoNotificationPreviewWindow(
        IServiceProvider services, CargoShipmentDirection direction)
    {
        InitializeComponent();
        _services      = services;
        _direction     = direction;
        _dialogService = services.GetRequiredService<IDialogService>();
    }

    /// <summary>Handler sonucundaki modeli ekrana yansıtır.</summary>
    public void Initialize(CargoNotificationModel model)
    {
        _model = model;
        ShipmentNumberBlock.Text = model.ShipmentNumber ?? "—";
        ReceiverBlock.Text       = model.ReceiverCompany ?? "—";
        PhoneBlock.Text          = string.IsNullOrWhiteSpace(model.TargetPhone)
            ? "Telefon bilgisi girilmemiş"
            : model.TargetPhone;
        MessageBodyBox.Text      = model.MessageBody;
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
            NotificationType = NotificationType.WhatsApp
        });

        if (!result.Success)
        {
            _dialogService.ShowError(result.ErrorMessage ?? "Beklenmeyen bir hata oluştu.");
            return;
        }

        WasMarkedPrepared            = true;
        MarkPreparedButton.IsEnabled = false; // çift işaretlemeyi önle

        _dialogService.ShowSuccess(
            $"'{_model.ShipmentNumber}' için bildirim durumu 'WhatsApp Hazır' olarak güncellendi.",
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

        // Boşluk, parantez, tire, artı → temizle
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digits)) return null;

        // 0532 123 45 67 → 11 hane, 0 ile başlar → 90532... (12 hane)
        if (digits.Length == 11 && digits.StartsWith("0"))
            return "90" + digits[1..];

        // 532 123 45 67 → 10 hane, ülke kodu yok → 90532... (12 hane)
        if (digits.Length == 10)
            return "90" + digits;

        // +90... veya 90... → zaten ülke kodu var
        return digits;
    }
}
