using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts;
using YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts.GetMailContactList;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Mail;

// WhatsAppContactListViewModel ile aynı desen; mail rehberinde firma filtresi yoktur
// (arama zaten firmayı kapsıyor), bu yüzden ComboBox senkron karmaşası da yoktur.
public class MailContactListViewModel : INotifyPropertyChanged
{
    private readonly GetMailContactListHandler _listHandler;

    private MailContactDto? _selected;
    private string _keyword = string.Empty;
    private bool _includeInactive;

    // Yükleme sürerken gelen istekler kuyruğa alınır; kuyruğa alınan tekrar
    // en son filtre durumunu uygular.
    private readonly ReloadCoordinator _loadCoordinator = new();

    public ObservableCollection<MailContactDto> Items { get; } = [];

    public MailContactDto? Selected
    {
        get => _selected;
        set { _selected = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSelected)); }
    }

    public bool HasSelected => _selected is not null;

    public string Keyword
    {
        get => _keyword;
        set { _keyword = value; OnPropertyChanged(); }
    }

    public bool IncludeInactive
    {
        get => _includeInactive;
        set
        {
            if (_includeInactive == value) return;

            _includeInactive = value;
            OnPropertyChanged();
            LoadAsync().Forget();
        }
    }

    public ICommand SearchCommand { get; }

    public MailContactListViewModel(GetMailContactListHandler listHandler)
    {
        _listHandler  = listHandler;
        SearchCommand = new RelayCommand(async () => await LoadAsync());
    }

    public Task LoadAsync() => _loadCoordinator.RunAsync(LoadCoreAsync);

    private async Task LoadCoreAsync()
    {
        var contacts = await _listHandler.HandleAsync(new GetMailContactListQuery
        {
            Search          = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim(),
            IncludeInactive = IncludeInactive
        });

        // Yenileme sonrası seçim korunur; aksi halde Düzenle/Sil butonları pasifleşir
        var previousId = _selected?.Id;

        Items.Clear();
        foreach (var c in contacts)
            Items.Add(c);

        Selected = previousId is null ? null : Items.FirstOrDefault(i => i.Id == previousId.Value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
