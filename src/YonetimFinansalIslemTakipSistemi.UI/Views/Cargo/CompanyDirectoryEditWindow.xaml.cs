using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Queries.GetCompanyDirectoryList;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

public partial class CompanyDirectoryEditWindow : Window
{
    private readonly CompanyDirectoryEditViewModel _vm;

    public CompanyDirectoryEditWindow(IServiceProvider services)
    {
        InitializeComponent();
        _vm = services.GetRequiredService<CompanyDirectoryEditViewModel>();
        DataContext = _vm;
        _vm.SaveCompleted += () => { DialogResult = true; Close(); };
    }

    public void InitializeForEdit(CompanyDirectoryDto dto) => _vm.Initialize(dto);

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
