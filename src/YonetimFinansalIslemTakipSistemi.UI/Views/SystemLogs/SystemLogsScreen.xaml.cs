using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.SystemLogs;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.SystemLogs;

public partial class SystemLogsScreen : UserControl
{

    /// <summary>
    /// Alt diyalogların sahibi AĞAÇTAN bulunur. Aynı ekran hem ince
    /// barındırıcı pencerede hem kabuk sekmesinde durabiliyor; sabit bir
    /// pencereye bağlanırsa diğerinde sahipsiz diyalog açardı.
    /// </summary>
    private Window? HostWindow => Window.GetWindow(this);
    private readonly IServiceProvider  _services;
    private readonly SystemLogsViewModel _vm;

    public SystemLogsScreen(IServiceProvider services)
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
        var detail = new SystemLogDetailWindow(_services, _vm.Selected.Id) { Owner = HostWindow };
        if (detail.ShowDialog() == true)
        {
            // Çözüldü işaretlenmiş olabilir — listeyi yenile
            _ = _vm.LoadAsync();
        }
    }
}
