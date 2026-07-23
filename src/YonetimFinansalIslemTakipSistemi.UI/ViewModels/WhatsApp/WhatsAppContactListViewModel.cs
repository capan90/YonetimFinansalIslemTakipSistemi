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
            _selectedCompany = value;
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

    public async Task LoadAsync()
    {
        var company = _selectedCompany == AllCompanies ? null : _selectedCompany;

        var contacts = await _listHandler.HandleAsync(new GetWhatsAppContactListQuery
        {
            Search          = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim(),
            Company         = company,
            IncludeInactive = IncludeInactive
        });

        Items.Clear();
        foreach (var c in contacts)
            Items.Add(c);

        await RefreshCompanyOptionsAsync();
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

        var current = _selectedCompany;
        CompanyOptions.Clear();
        CompanyOptions.Add(AllCompanies);
        foreach (var c in companies)
            CompanyOptions.Add(c);

        // Seçim listede kaldıysa koru; kalktıysa "Tümü"ye dön (setter'ı tetiklemeden)
        _selectedCompany = CompanyOptions.Contains(current) ? current : AllCompanies;
        OnPropertyChanged(nameof(SelectedCompany));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
