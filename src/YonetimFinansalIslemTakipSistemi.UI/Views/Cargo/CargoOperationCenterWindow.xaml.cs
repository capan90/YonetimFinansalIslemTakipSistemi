using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.IO;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Commands.QuickUpdateCargoStatus;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoShipmentList;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Label.GenerateCargoLabel;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification.GenerateCargoNotification;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

/// <summary>
/// Seçili bir kargo kaydı için tüm operasyonları tek pencerede sunar.
/// Mevcut handler'ları orchestrate eder; yeni business logic içermez.
/// </summary>
public partial class CargoOperationCenterWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly IDialogService   _dialogService;
    private readonly CargoShipmentDto _dto;

    /// <summary>
    /// Status değiştirildi veya bildirim hazırlandıysa true.
    /// Liste ekranı bu değere göre yenileme kararı verir.
    /// </summary>
    public bool WasModified { get; private set; }

    public CargoOperationCenterWindow(IServiceProvider services, CargoShipmentDto dto)
    {
        InitializeComponent();
        _services      = services;
        _dialogService = services.GetRequiredService<IDialogService>();
        _dto           = dto;

        var direction = dto.Direction == CargoShipmentDirection.Incoming
            ? "Gelen Kargo" : "Giden Kargo";
        Title = $"Operasyon Merkezi — {direction}";

        PopulateInfo(dto.StatusDisplay, dto.NotificationStatusDisplay);

        // Takip butonu: takip linki yoksa devre dışı
        TrackButton.IsEnabled = !string.IsNullOrWhiteSpace(dto.TrackingUrl);

        // Gelen kargoda bildirim butonları henüz aktif değil
        if (dto.Direction == CargoShipmentDirection.Incoming)
        {
            const string tip = "Gelen kargo bildirimleri daha sonra aktif edilecektir.";
            WhatsAppButton.IsEnabled = false;
            WhatsAppButton.ToolTip   = tip;
            MailButton.IsEnabled     = false;
            MailButton.ToolTip       = tip;
        }
    }

    private void PopulateInfo(string statusDisplay, string notificationDisplay)
    {
        ShipmentNumberBlock.Text = _dto.ShipmentNumber ?? "—";
        PartyBlock.Text          = _dto.DisplayParty   ?? "—";
        PriorityBlock.Text       = _dto.PriorityDisplay;
        StatusBlock.Text         = statusDisplay;
        NotificationBlock.Text   = notificationDisplay;
    }

    // ── Etiket Önizle ────────────────────────────────────────────────────

    private async void LabelButton_Click(object sender, RoutedEventArgs e)
    {
        var handler = _services.GetRequiredService<GenerateCargoLabelHandler>();
        var result  = await handler.HandleAsync(new GenerateCargoLabelRequest
        {
            Id        = _dto.Id,
            Direction = _dto.Direction
        });

        if (!result.Success)
        {
            _dialogService.ShowError(result.ErrorMessage ?? "Etiket oluşturulamadı.");
            return;
        }

        var safeName = (_dto.ShipmentNumber ?? _dto.Id.ToString()[..8])
            .Replace('/', '-').Replace('\\', '-');
        var tempPath = Path.Combine(Path.GetTempPath(), $"kargo-etiketi-{safeName}.pdf");
        await File.WriteAllBytesAsync(tempPath, result.Data!);
        Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
    }

    // ── WhatsApp Hazırla ──────────────────────────────────────────────────

    private async void WhatsAppButton_Click(object sender, RoutedEventArgs e)
        => await OpenNotificationPreviewAsync(NotificationType.WhatsApp);

    // ── Mail Hazırla ──────────────────────────────────────────────────────

    private async void MailButton_Click(object sender, RoutedEventArgs e)
        => await OpenNotificationPreviewAsync(NotificationType.Mail);

    private async Task OpenNotificationPreviewAsync(NotificationType notificationType)
    {
        var handler = _services.GetRequiredService<GenerateCargoNotificationHandler>();
        var result  = await handler.HandleAsync(new GenerateCargoNotificationRequest
        {
            CargoShipmentId  = _dto.Id,
            Direction        = _dto.Direction,
            NotificationType = notificationType
        });

        if (!result.Success)
        {
            _dialogService.ShowError(result.ErrorMessage ?? "Bildirim hazırlanamadı.");
            return;
        }

        var preview = new CargoNotificationPreviewWindow(_services, _dto.Direction, notificationType)
        {
            Owner = this
        };
        preview.Initialize(result.Data!);
        preview.ShowDialog();

        if (preview.WasMarkedPrepared)
        {
            WasModified = true;
            var newNotifDisplay = notificationType == NotificationType.Mail
                ? "Mail Hazır" : "WhatsApp Hazır";
            NotificationBlock.Text = newNotifDisplay;

            // Kullanıcı önizleme penceresi kapandıktan sonra Operation Center'da özet görsün
            var msg = notificationType == NotificationType.Mail
                ? "Mail gönderildi ve bildirim durumu güncellendi."
                : "WhatsApp Web açıldı ve bildirim durumu güncellendi.";
            ShowStatusMessage(msg);
        }
    }

    // ── Takip Linkini Aç ─────────────────────────────────────────────────

    private void TrackButton_Click(object sender, RoutedEventArgs e)
    {
        var url = _dto.TrackingUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            _dialogService.ShowWarning("Bu kargo için takip linki bulunmamaktadır.", "Takip Et");
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Takip linki açılamadı: {ex.Message}");
        }
    }

    // ── Durum Değiştir ────────────────────────────────────────────────────

    private async void StatusButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new QuickUpdateStatusDialog(_dto.StatusDisplay, _dto.Status, _dto.Direction)
        {
            Owner = this
        };
        if (dialog.ShowDialog() != true || dialog.SelectedStatus is null) return;

        var userContext = _services.GetRequiredService<IUserContext>();
        var handler     = _services.GetRequiredService<QuickUpdateCargoStatusHandler>();

        var result = await handler.HandleAsync(new QuickUpdateCargoStatusRequest
        {
            Id              = _dto.Id,
            Direction       = _dto.Direction,
            NewStatus       = dialog.SelectedStatus.Value,
            UpdatedByUserId = userContext.UserId
        });

        if (!result.Success)
        {
            _dialogService.ShowError(result.ErrorMessage ?? "Beklenmeyen bir hata oluştu.");
            return;
        }

        WasModified = true;
        // Durum etiketini anlık güncelle
        StatusBlock.Text = DisplayStatus(dialog.SelectedStatus.Value);
        // Yeni duruma göre TrackButton durumunu korumaya gerek yok (TrackingUrl değişmedi)
    }

    private void ShowStatusMessage(string message)
    {
        OpStatusBlock.Text        = message;
        OpStatusBorder.Visibility = Visibility.Visible;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private string DisplayStatus(CargoShipmentStatus s) => s switch
    {
        CargoShipmentStatus.Draft              => _dto.Direction == CargoShipmentDirection.Incoming ? "Bekleniyor" : "Gönderime Hazır",
        CargoShipmentStatus.Prepared           => "Gönderime Hazır",
        CargoShipmentStatus.HandedToCargo      => "Kargoya Teslim Edildi",
        CargoShipmentStatus.Shipped            => "Gönderildi",
        CargoShipmentStatus.Waiting            => "Bekleniyor",
        CargoShipmentStatus.Received           => "Teslim Alındı",
        CargoShipmentStatus.PersonnelDelivered => "Personele Teslim Edildi",
        CargoShipmentStatus.Delivered          => "Teslim Edildi",
        CargoShipmentStatus.Cancelled          => "İptal",
        _                                      => s.ToString()
    };
}
