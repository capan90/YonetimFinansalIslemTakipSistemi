using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.UI.Abstractions;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Reports;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Reports;

public partial class ReportScreen : UserControl
{

    /// <summary>
    /// Alt diyalogların sahibi AĞAÇTAN bulunur. Aynı ekran hem ince
    /// barındırıcı pencerede hem kabuk sekmesinde durabiliyor; sabit bir
    /// pencereye bağlanırsa diğerinde sahipsiz diyalog açardı.
    /// </summary>
    private Window? HostWindow => Window.GetWindow(this);
    private readonly IServiceProvider _services;
    private readonly ReportViewModel  _vm;

    public ReportScreen(IServiceProvider services)
    {
        InitializeComponent();
        _services   = services;
        _vm         = services.GetRequiredService<ReportViewModel>();
        DataContext = _vm;

        ScreenData.Bind(this, () => _vm.LoadAsync());
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.LastReportDto is null) return;

        new ReportPreviewWindow(
                _vm.LastReportDto,
                _services.GetRequiredService<IReportExportService>(),
                _services.GetRequiredService<IDialogService>(),
                _services.GetRequiredService<IAuditLogService>(),
                _services.GetRequiredService<ISystemLogService>(),
                _services.GetRequiredService<IUserContext>())
            { Owner = HostWindow }
            .ShowDialog();
    }
}
