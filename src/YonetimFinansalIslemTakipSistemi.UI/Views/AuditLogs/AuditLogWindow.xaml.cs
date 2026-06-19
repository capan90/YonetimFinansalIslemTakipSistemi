using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.AuditLogs;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.AuditLogs;

public partial class AuditLogWindow : Window
{
    private readonly AuditLogViewModel _vm;

    public AuditLogWindow(IServiceProvider services)
    {
        InitializeComponent();
        _vm         = services.GetRequiredService<AuditLogViewModel>();
        DataContext = _vm;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
        => await _vm.LoadAsync();
}
