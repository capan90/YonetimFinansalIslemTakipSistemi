using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Queries.GetCompanyDirectoryList;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;

public class CompanyDirectoryListViewModel : INotifyPropertyChanged
{
    private readonly GetCompanyDirectoryListHandler _listHandler;

    private string _keyword = string.Empty;
    private CompanyDirectoryDto? _selected;
    private ObservableCollection<CompanyDirectoryDto> _items = [];

    public string Keyword
    {
        get => _keyword;
        set { _keyword = value; OnPropertyChanged(); }
    }

    public CompanyDirectoryDto? Selected
    {
        get => _selected;
        set { _selected = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSelected)); }
    }

    public bool HasSelected => _selected is not null;

    public ObservableCollection<CompanyDirectoryDto> Items
    {
        get => _items;
        private set { _items = value; OnPropertyChanged(); }
    }

    public ICommand SearchCommand { get; }

    public CompanyDirectoryListViewModel(GetCompanyDirectoryListHandler listHandler)
    {
        _listHandler  = listHandler;
        SearchCommand = new RelayCommand(async () => await LoadAsync());
    }

    public async Task LoadAsync()
    {
        var result = await _listHandler.HandleAsync(new GetCompanyDirectoryListQuery
        {
            Keyword = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword
        });

        Items = new ObservableCollection<CompanyDirectoryDto>(result);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
