using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Reports;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Reports;

public partial class ReportWindow : Window
{
    private readonly ReportViewModel _vm;

    public ReportWindow(IServiceProvider services)
    {
        InitializeComponent();
        _vm         = services.GetRequiredService<ReportViewModel>();
        DataContext = _vm;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
        => await _vm.LoadAsync();
}
