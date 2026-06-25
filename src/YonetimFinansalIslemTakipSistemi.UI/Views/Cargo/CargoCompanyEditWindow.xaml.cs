using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Queries.GetCargoCompanyList;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Cargo;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Cargo;

public partial class CargoCompanyEditWindow : Window
{
    private static readonly Regex PhoneAllowed = new(@"^[0-9 +\-(),]+$", RegexOptions.Compiled);
    private readonly CargoCompanyEditViewModel _vm;

    public CargoCompanyEditWindow(IServiceProvider services)
    {
        InitializeComponent();
        _vm = services.GetRequiredService<CargoCompanyEditViewModel>();
        DataContext = _vm;
        _vm.SaveCompleted += () => { DialogResult = true; Close(); };
    }

    public void InitializeForEdit(CargoCompanyDto dto) => _vm.Initialize(dto);

    private void PhoneTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        => e.Handled = !PhoneAllowed.IsMatch(e.Text);

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}
