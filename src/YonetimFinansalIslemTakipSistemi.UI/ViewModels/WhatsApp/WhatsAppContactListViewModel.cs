using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts;
using YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.GetWhatsAppContactList;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.WhatsApp;

public class WhatsAppContactListViewModel : INotifyPropertyChanged
{
    private const string AllCompanies = "Tümü";

    private readonly GetWhatsAppContactListHandler _listHandler;

    private WhatsAppContactDto? _selected;
    private string _keyword = string.Empty;
    private string _selectedCompany = AllCompanies;
    private bool _includeInactive;

    // Firma ComboBox'ının kaynağı yenilenirken WPF SelectedItem'ı null'a düşürür ve bunu
    // TwoWay binding ile geri yazar. Bu geri yazma yeniden yükleme tetiklememelidir.
    private bool _suppressCompanyReload;

    // Oturum boyunca tek DbContext paylaşılır; eşzamanlı sorgu
    // "A second operation was started on this context instance" hatasına yol açar.
    private bool _isLoading;
    private bool _reloadRequested;

    public ObservableCollection<WhatsAppContactDto> Items { get; } = [];
    public ObservableCollection<string> CompanyOptions { get; } = [AllCompanies];

    public WhatsAppContactDto? Selected
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

    public string SelectedCompany
    {
        get => _selectedCompany;
        set
        {
            // Liste yenilenirken gelen null geri yazması yok sayılır; nihai değeri
            // RefreshCompanyOptionsAsync belirler. Aksi halde
            // Load → CompanyOptions.Clear() → setter → Load ... sonsuz döngüsü oluşur.
            if (_suppressCompanyReload) return;

            var normalized = string.IsNullOrEmpty(value) ? AllCompanies : value;
            if (normalized == _selectedCompany) return;

            _selectedCompany = normalized;
            OnPropertyChanged();
            // Firma filtresi seçilince liste otomatik yenilenir; hata Forget ile UI'a taşınır
            LoadAsync().Forget();
        }
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

    public WhatsAppContactListViewModel(GetWhatsAppContactListHandler listHandler)
    {
        _listHandler  = listHandler;
        SearchCommand = new RelayCommand(async () => await LoadAsync());
    }

    /// <summary>
    /// Listeyi yeniler. Yükleme sürerken gelen istekler kuyruğa alınır (tek DbContext
    /// paylaşıldığı için eşzamanlı sorgu çalıştırılmaz), böylece son istek de uygulanır.
    /// </summary>
    public async Task LoadAsync()
    {
        if (_isLoading) { _reloadRequested = true; return; }

        _isLoading = true;
        try
        {
            do
            {
                _reloadRequested = false;
                await LoadCoreAsync();
            }
            while (_reloadRequested);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadCoreAsync()
    {
        var company = _selectedCompany == AllCompanies ? null : _selectedCompany;

        var contacts = await _listHandler.HandleAsync(new GetWhatsAppContactListQuery
        {
            Search          = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim(),
            Company         = company,
            IncludeInactive = IncludeInactive
        });

        // Yenileme sonrası seçim korunur; aksi halde DataGrid seçimi düşer ve
        // Düzenle/Sil butonları pasifleşir.
        var previousId = _selected?.Id;

        Items.Clear();
        foreach (var c in contacts)
            Items.Add(c);

        await RefreshCompanyOptionsAsync();

        Selected = previousId is null ? null : Items.FirstOrDefault(i => i.Id == previousId.Value);
    }

    /// <summary>Firma filtresi seçenekleri tüm rehberden türetilir (arama filtresinden bağımsız).</summary>
    private async Task RefreshCompanyOptionsAsync()
    {
        var all = await _listHandler.HandleAsync(new GetWhatsAppContactListQuery { IncludeInactive = true });

        var companies = all
            .Where(c => !string.IsNullOrWhiteSpace(c.Company))
            .Select(c => c.Company!)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        // Seçenekler değişmediyse koleksiyona dokunulmaz: Clear() ComboBox seçimini
        // null'a düşürüp gereksiz yeniden yükleme zinciri başlatır.
        if (CompanyOptions.Count == companies.Count + 1 &&
            CompanyOptions.Skip(1).SequenceEqual(companies))
            return;

        var current = _selectedCompany;

        _suppressCompanyReload = true;
        try
        {
            CompanyOptions.Clear();
            CompanyOptions.Add(AllCompanies);
            foreach (var c in companies)
                CompanyOptions.Add(c);

            // Seçim listede kaldıysa koru; kalktıysa "Tümü"ye dön (setter'ı tetiklemeden)
            _selectedCompany = CompanyOptions.Contains(current) ? current : AllCompanies;
        }
        finally
        {
            _suppressCompanyReload = false;
        }

        OnPropertyChanged(nameof(SelectedCompany));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
