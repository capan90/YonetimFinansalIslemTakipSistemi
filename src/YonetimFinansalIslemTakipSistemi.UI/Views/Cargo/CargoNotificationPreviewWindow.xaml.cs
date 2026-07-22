using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Notification.MarkCargoNotificationPrepared;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.GetWhatsAppContactList;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.Views.WhatsApp;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

public partial class CargoNotificationPreviewWindow : Window
{
    private readonly IServiceProvider       _services;
    private readonly IDialogService         _dialogService;
    private readonly CargoShipmentDirection _direction;
    private readonly NotificationType       _notificationType;
    private          CargoNotificationModel _model = null!;

    // WhatsApp rehber seçimi
    private readonly List<ContactPickItem> _allContacts = [];
    private readonly ObservableCollection<ContactPickItem> _selectedContacts = [];

    /// <summary>Rehber checkbox listesi satırı. Display: "Ad (Firma)".</summary>
    private sealed class ContactPickItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public required WhatsAppContactDto Contact { get; init; }
        public string Display => Contact.DisplayText;
        public string Phone   => Contact.Phone;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

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

        // Gönderici adresi DB'den async yüklenir; UI thread bloke edilmez
        FromBlock.Text      = "...";
        ToTextBox.Text      = model.TargetEmail ?? string.Empty;
        SubjectTextBox.Text = model.Subject ?? $"Kargo Bilgilendirme - {model.ShipmentNumber}";

        MailFieldsPanel.Visibility = Visibility.Visible;
        MailSendButton.Visibility  = Visibility.Visible;

        // Alıcı alanı boşsa veya geçersizse gönder butonu devre dışı; kullanıcı manuel doldurabilir
        MailSendButton.IsEnabled = IsValidEmail(ToTextBox.Text.Trim());

        Loaded += async (_, _) => await LoadSenderEmailAsync();
    }

    private async Task LoadSenderEmailAsync()
    {
        var mailSettings = _services.GetRequiredService<IMailSettingsService>();
        var settings     = await mailSettings.GetAsync();
        FromBlock.Text   = settings?.SenderEmail ?? settings?.Username ?? "(ayarlanmamış)";
    }

    private void InitializeWhatsAppMode(CargoNotificationModel model)
    {
        Title           = "WhatsApp Mesajı Hazırla";
        TitleBlock.Text = "💬 WhatsApp Mesajı Hazırla";

        PhoneLabelBlock.Visibility = Visibility.Visible;
        PhoneTextBox.Visibility = Visibility.Visible;
        PhoneTextBox.Text = !string.IsNullOrWhiteSpace(model.TargetPhone)
            ? model.TargetPhone : string.Empty;

        WhatsAppWebButton.Visibility = Visibility.Visible;

        // Rehber paneli: aranabilir çoklu seçim + chip listesi.
        // Kişi listesi Popup içinde açılır-kapanır; panel ekranı sürekli kaplamaz.
        ContactsPanel.Visibility = Visibility.Visible;
        SelectedChips.ItemsSource = _selectedContacts;
        Height = 720;

        Loaded += async (_, _) => await LoadContactsAsync();
    }

    // ── WhatsApp rehber seçimi ────────────────────────────────────────────

    private async Task LoadContactsAsync(Guid? autoSelectId = null)
    {
        var listHandler = _services.GetRequiredService<GetWhatsAppContactListHandler>();
        // Pasif/silinmiş kayıtlar seçim listesinde görünmez
        var contacts = await listHandler.HandleAsync(new GetWhatsAppContactListQuery());

        // Önceki seçimleri koru
        var selectedIds = _selectedContacts.Select(x => x.Contact.Id).ToHashSet();

        _allContacts.Clear();
        _selectedContacts.Clear();
        foreach (var c in contacts)
        {
            var item = new ContactPickItem { Contact = c };
            if (selectedIds.Contains(c.Id) || c.Id == autoSelectId)
            {
                item.IsSelected = true;
                _selectedContacts.Add(item);
            }
            _allContacts.Add(item);
        }

        ApplyContactFilter();
        UpdatePhoneBoxFromSelection();
    }

    private void ApplyContactFilter()
    {
        var term = ContactSearchBox.Text;
        var filteredDtos = WhatsAppContactSearch
            .Filter(_allContacts.Select(x => x.Contact).ToList(), term)
            .Select(x => x.Id)
            .ToHashSet();

        ContactListBox.ItemsSource = _allContacts
            .Where(x => filteredDtos.Contains(x.Contact.Id))
            .ToList();
    }

    private void ContactSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyContactFilter();
        // Yazmaya başlayınca sonuç listesi açılır (başlangıçta kapalıdır)
        if (IsLoaded)
            ContactPopup.IsOpen = true;
    }

    private void ContactSearchBox_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        => ContactPopup.IsOpen = true;

    private void DropDownButton_Click(object sender, RoutedEventArgs e)
    {
        ContactPopup.IsOpen = true;
        ContactSearchBox.Focus();
    }

    private void ContactCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ContactPickItem item) return;

        if (item.IsSelected && !_selectedContacts.Contains(item))
            _selectedContacts.Add(item);
        else if (!item.IsSelected)
            _selectedContacts.Remove(item);

        UpdatePhoneBoxFromSelection();
    }

    private void RemoveChip_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not ContactPickItem item) return;
        item.IsSelected = false; // checkbox binding'i Unchecked handler'ını tetikler
        _selectedContacts.Remove(item);
        UpdatePhoneBoxFromSelection();
    }

    /// <summary>
    /// Rehberden kişi seçiliyken telefon alanı salt okunurdur — numara düzeltmesi
    /// yalnızca WhatsApp Rehberi ekranından yapılır.
    /// </summary>
    private void UpdatePhoneBoxFromSelection()
    {
        switch (_selectedContacts.Count)
        {
            case 0:
                PhoneTextBox.IsReadOnly = false;
                PhoneTextBox.Text = _model?.TargetPhone ?? string.Empty;
                PhoneTextBox.ToolTip = "WhatsApp alıcı numarası — yalnızca bu gönderim için kullanılır";
                break;
            case 1:
                PhoneTextBox.IsReadOnly = true;
                PhoneTextBox.Text = _selectedContacts[0].Phone;
                PhoneTextBox.ToolTip = "Numara rehberden gelir — düzeltmek için WhatsApp Rehberi ekranını kullanın";
                break;
            default:
                PhoneTextBox.IsReadOnly = true;
                PhoneTextBox.Text = $"{_selectedContacts.Count} kişi seçildi";
                PhoneTextBox.ToolTip = "Numaralar rehberden gelir — düzeltmek için WhatsApp Rehberi ekranını kullanın";
                break;
        }
    }

    private async void AddContactButton_Click(object sender, RoutedEventArgs e)
    {
        // Hızlı ekleme: kişi ortak rehbere kaydedilir, liste yenilenir, yeni kişi otomatik seçilir
        var form = new WhatsAppContactEditWindow(_services) { Owner = this };
        if (form.ShowDialog() == true && form.SavedContact is not null)
            await LoadContactsAsync(autoSelectId: form.SavedContact.Id);
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
        // Alıcılar: rehberden seçilen kişiler; hiç seçim yoksa manuel telefon (mevcut akış korunur)
        var recipients = new List<(string Name, string Phone)>();

        if (_selectedContacts.Count > 0)
        {
            foreach (var item in _selectedContacts)
            {
                var normalized = NormalizePhone(item.Phone);
                if (!string.IsNullOrWhiteSpace(normalized))
                    recipients.Add((item.Contact.FullName, normalized));
            }
        }
        else
        {
            var inputPhone = PhoneTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(inputPhone))
            {
                _dialogService.ShowWarning("Lütfen rehberden kişi seçiniz veya alıcı telefon numarasını giriniz.", "Alıcı Eksik");
                return;
            }

            var phone = NormalizePhone(inputPhone);
            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 10)
            {
                _dialogService.ShowWarning("Lütfen geçerli bir telefon numarası giriniz (örn: 0532 123 45 67).", "Geçersiz Telefon");
                return;
            }

            recipients.Add(("Manuel numara", phone));
        }

        if (recipients.Count == 0)
        {
            _dialogService.ShowWarning("Geçerli alıcı bulunamadı.", "Alıcı Eksik");
            return;
        }

        WhatsAppWebButton.IsEnabled = false;

        // wa.me toplu gönderimi desteklemez — her alıcı için ayrı WhatsApp Web açılışı yapılır
        var encoded   = Uri.EscapeDataString(_model?.MessageBody ?? string.Empty);
        var succeeded = new List<string>();
        var failed    = new List<string>();

        foreach (var (name, phone) in recipients)
        {
            try
            {
                var url = $"https://wa.me/{phone}?text={encoded}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                succeeded.Add(name);
                // Ardışık sekme açılışları arasında tarayıcıya nefes payı
                if (recipients.Count > 1)
                    await Task.Delay(400);
            }
            catch
            {
                failed.Add(name);
            }
        }

        if (succeeded.Count == 0)
        {
            WhatsAppWebButton.IsEnabled = true;
            _dialogService.ShowError("WhatsApp Web açılamadı. Varsayılan tarayıcıyı kontrol edin.");
            return;
        }

        if (failed.Count > 0)
            _dialogService.ShowWarning(
                $"İşlenen kişiler: {string.Join(", ", succeeded)}\n\nAçılamayanlar: {string.Join(", ", failed)}",
                "Bazı Alıcılar İşlenemedi");

        // En az bir alıcı işlendi → bildirim durumu güncellenir; işlenenler audit'e yazılır
        await MarkPreparedAsync(string.Join(", ", succeeded));

        var summary = recipients.Count == 1
            ? "WhatsApp Web açıldı. Bildirim durumu güncellendi."
            : $"{succeeded.Count}/{recipients.Count} kişi için WhatsApp Web açıldı. Bildirim durumu güncellendi.";
        await ShowSuccessAndCloseAsync(summary);
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
        MailSendButton.Content   = "⏳ Mail Gönderiliyor...";

        var mailSender       = _services.GetRequiredService<ICargoMailSenderService>();
        var cc               = string.IsNullOrWhiteSpace(CcTextBox.Text) ? null : CcTextBox.Text.Trim();
        var subject          = SubjectTextBox.Text.Trim();
        var (success, error) = await mailSender.SendAsync(to, cc, subject, _model.MessageBody);

        if (!success)
        {
            MailSendButton.IsEnabled = true;
            MailSendButton.Content   = "📧 Mail Gönder";
            // Teknik detay log dosyasına yazılır; kullanıcıya kısa ve anlaşılır mesaj gösterilir
            _dialogService.ShowError(BuildMailErrorMessage(error), "Mail Gönderilemedi");
            return;
        }

        // Başarılı gönderim → bildirim durumunu otomatik güncelle, başarı bildirimi göster
        await MarkPreparedAsync();
        await ShowSuccessAndCloseAsync("Mail başarıyla gönderildi. Bildirim durumu güncellendi.");
    }

    // ── Ortak: durumu güncelle ────────────────────────────────────────────

    private async Task MarkPreparedAsync(string? recipientSummary = null)
    {
        var handler = _services.GetRequiredService<MarkCargoNotificationPreparedHandler>();
        var result  = await handler.HandleAsync(new MarkCargoNotificationPreparedRequest
        {
            CargoShipmentId  = _model.ShipmentId,
            Direction        = _direction,
            NotificationType = _notificationType,
            RecipientSummary = recipientSummary
        });

        if (!result.Success)
        {
            _dialogService.ShowError(result.ErrorMessage ?? "Bildirim durumu güncellenemedi.");
            return;
        }

        WasMarkedPrepared = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void ToTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (MailSendButton is not null)
        {
            var email = ToTextBox.Text.Trim();
            MailSendButton.IsEnabled = IsValidEmail(email);
        }
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(email, 
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase, 
                TimeSpan.FromMilliseconds(250));
        }
        catch
        {
            return false;
        }
    }

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
