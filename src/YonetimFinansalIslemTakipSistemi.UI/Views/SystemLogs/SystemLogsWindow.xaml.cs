using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.SystemLogs;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.SystemLogs;

public partial class SystemLogsWindow : Window
{
    private readonly IServiceProvider  _services;
    private readonly SystemLogsViewModel _vm;

    public SystemLogsWindow(IServiceProvider services)
    {
        _services = services;
        _vm       = services.GetRequiredService<SystemLogsViewModel>();
        InitializeComponent();
        DataContext = _vm;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await _vm.LoadAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await _vm.LoadAsync();
    }

    private void MainGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Satır dışına tıklandıysa form açma
        var hit = e.OriginalSource as DependencyObject;
        while (hit is not null && hit is not DataGridRow)
            hit = VisualTreeHelper.GetParent(hit);
        if (hit is null) return;

        OpenDetail();
    }

    private void OpenDetail()
    {
        if (_vm.Selected is null) return;
        var detail = new SystemLogDetailWindow(_services, _vm.Selected.Id) { Owner = this };
        if (detail.ShowDialog() == true)
        {
            // Çözüldü işaretlenmiş olabilir — listeyi yenile
            _ = _vm.LoadAsync();
        }
    }
}
