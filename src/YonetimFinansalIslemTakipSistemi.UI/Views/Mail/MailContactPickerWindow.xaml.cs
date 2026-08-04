using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts;
using YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts.GetMailContactList;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Mail;

/// <summary>
/// Mail rehberinden çoklu adres seçimi. Alıcı ve CC alanları aynı pencereyi kullanır;
/// başlık ve zaten seçili adresler parametreyle verilir.
/// </summary>
public partial class MailContactPickerWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly List<ContactPickItem> _allContacts = [];

    /// <summary>Kullanıcı "Ekle" dediğinde seçili adresler (normalize).</summary>
    public IReadOnlyList<string> SelectedEmails { get; private set; } = [];

    /// <summary>Rehber listesi satırı; checkbox durumunu taşır.</summary>
    private sealed class ContactPickItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public required MailContactDto Contact { get; init; }
        public string Display => Contact.DisplayText;
        public string Email   => Contact.Email;

        /// <summary>XAML'de converter kurmadan rozeti göstermek için hazır değer.</summary>
        public Visibility DefaultCcBadgeVisibility =>
            Contact.IsDefaultCc ? Visibility.Visible : Visibility.Collapsed;

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

    public MailContactPickerWindow(IServiceProvider services, string title, IEnumerable<string>? preSelected = null)
    {
        InitializeComponent();
        _services       = services;
        Title           = title;
        TitleBlock.Text = title;

        var preSelectedSet = new HashSet<string>(
            preSelected ?? [], StringComparer.OrdinalIgnoreCase);

        Loaded += async (_, _) => await LoadContactsAsync(preSelectedSet);
    }

    private async Task LoadContactsAsync(HashSet<string> preSelected, Guid? autoSelectId = null)
    {
        var handler = _services.GetRequiredService<GetMailContactListHandler>();
        // Pasif/silinmiş kayıtlar seçim listesinde görünmez
        var contacts = await handler.HandleAsync(new GetMailContactListQuery());

        _allContacts.Clear();
        foreach (var c in contacts)
        {
            _allContacts.Add(new ContactPickItem
            {
                Contact    = c,
                IsSelected = preSelected.Contains(c.Email) || c.Id == autoSelectId
            });
        }

        ApplyFilter();
        UpdateSelectionCount();
    }

    private void ApplyFilter()
    {
        var matchingIds = MailContactSearch
            .Filter(_allContacts.Select(x => x.Contact).ToList(), SearchBox.Text)
            .Select(x => x.Id)
            .ToHashSet();

        // Seçim durumu _allContacts üzerinde tutulur; filtre yalnızca görünürlüğü değiştirir
        ContactListBox.ItemsSource = _allContacts
            .Where(x => matchingIds.Contains(x.Contact.Id))
            .ToList();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void ContactCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateSelectionCount();

    private void UpdateSelectionCount()
    {
        var count = _allContacts.Count(x => x.IsSelected);
        SelectionCountBlock.Text = count == 0 ? "" : $"{count} kişi seçildi";
    }

    private async void AddContactButton_Click(object sender, RoutedEventArgs e)
    {
        // Hızlı ekleme: kişi ortak rehbere kaydedilir, liste yenilenir, yeni kişi otomatik seçilir.
        // Mevcut seçimler korunur — kullanıcı baştan işaretlemek zorunda kalmaz.
        var currentSelection = _allContacts
            .Where(x => x.IsSelected)
            .Select(x => x.Email)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var form = new MailContactEditWindow(_services) { Owner = this };
        if (form.ShowDialog() == true && form.SavedContact is not null)
            await LoadContactsAsync(currentSelection, autoSelectId: form.SavedContact.Id);
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedEmails = _allContacts
            .Where(x => x.IsSelected)
            .Select(x => x.Email)
            .ToList();

        DialogResult = true;
        Close();
    }
}
