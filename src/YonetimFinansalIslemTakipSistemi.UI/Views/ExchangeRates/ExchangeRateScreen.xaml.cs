using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
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

        ScreenData.Bind(this, () => _vm.LoadAsync());
    }
}
