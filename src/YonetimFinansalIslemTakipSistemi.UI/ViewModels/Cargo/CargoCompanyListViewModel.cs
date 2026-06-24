using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Queries.GetCargoCompanyList;
using YonetimFinansalIslemTakipSistemi.UI.Common;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;

public class CargoCompanyListViewModel : INotifyPropertyChanged
{
    private readonly GetCargoCompanyListHandler _listHandler;

    private string _keyword = string.Empty;
    private CargoCompanyDto? _selected;
    private ObservableCollection<CargoCompanyDto> _items = [];

    public string Keyword
    {
        get => _keyword;
        set { _keyword = value; OnPropertyChanged(); }
    }

    public CargoCompanyDto? Selected
    {
        get => _selected;
        set { _selected = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSelected)); }
    }

    public bool HasSelected => _selected is not null;

    public ObservableCollection<CargoCompanyDto> Items
    {
        get => _items;
        private set { _items = value; OnPropertyChanged(); }
    }

    public ICommand SearchCommand { get; }

    public CargoCompanyListViewModel(GetCargoCompanyListHandler listHandler)
    {
        _listHandler  = listHandler;
        SearchCommand = new RelayCommand(async () => await LoadAsync());
    }

    public async Task LoadAsync()
    {
        var result = await _listHandler.HandleAsync(new GetCargoCompanyListQuery
        {
            Keyword = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword
        });

        Items = new ObservableCollection<CargoCompanyDto>(result);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
