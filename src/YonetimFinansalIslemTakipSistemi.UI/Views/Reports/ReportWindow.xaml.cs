using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Reports;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Reports;

public partial class ReportWindow : Window
{
    private readonly IServiceProvider _services;
    private readonly ReportViewModel  _vm;

    public ReportWindow(IServiceProvider services)
    {
        InitializeComponent();
        _services   = services;
        _vm         = services.GetRequiredService<ReportViewModel>();
        DataContext = _vm;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
        => await _vm.LoadAsync();

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.LastReportDto is null) return;

        new ReportPreviewWindow(
                _vm.LastReportDto,
                _services.GetRequiredService<IReportExportService>(),
                _services.GetRequiredService<IDialogService>())
            { Owner = this }
            .ShowDialog();
    }
}
