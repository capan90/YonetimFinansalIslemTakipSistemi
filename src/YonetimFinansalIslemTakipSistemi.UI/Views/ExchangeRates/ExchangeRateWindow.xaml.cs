using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.ExchangeRates;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.ExchangeRates;

public partial class ExchangeRateWindow : Window
{
    private readonly ExchangeRateViewModel _vm;

    public ExchangeRateWindow(IServiceProvider services)
    {
        InitializeComponent();
        _vm         = services.GetRequiredService<ExchangeRateViewModel>();
        DataContext = _vm;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
        => _ = _vm.LoadAsync();
}
