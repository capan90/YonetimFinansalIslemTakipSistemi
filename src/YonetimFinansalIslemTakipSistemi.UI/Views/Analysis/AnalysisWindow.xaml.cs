using System.Windows;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Analysis;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Analysis;

public partial class AnalysisWindow : Window
{
    private readonly AnalysisViewModel _vm;

    public AnalysisWindow(AnalysisViewModel vm)
    {
        InitializeComponent();
        _vm         = vm;
        DataContext = vm;

        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
