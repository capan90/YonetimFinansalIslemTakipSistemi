using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.ExchangeRates;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.ExchangeRates;

public partial class ExchangeRateScreen : UserControl
{
    private readonly ExchangeRateViewModel _vm;

    public ExchangeRateScreen(IServiceProvider services)
    {
        InitializeComponent();
        _vm         = services.GetRequiredService<ExchangeRateViewModel>();
        DataContext = _vm;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
        => _ = _vm.LoadAsync();
}
