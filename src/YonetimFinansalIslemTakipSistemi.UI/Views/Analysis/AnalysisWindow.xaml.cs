using System.Windows;
using YonetimFinansalIslemTakipSistemi.UI.Common;
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

        // Grafik SkiaSharp ile çizilir ve DynamicResource'u görmez: tema
        // değişince seriler elle yeniden kurulmalı, yoksa eski renkte donar.
        ChartPalette.ThemeChanged += OnThemeChanged;
        Unloaded += (_, _) => ChartPalette.ThemeChanged -= OnThemeChanged;

        Loaded += async (_, _) => await _vm.LoadAsync();
    }

    // Boş durum görünürlüğü XAML'de DataTrigger ile bağlıdır (HasTrendData);
    // burada yalnızca yeniden boyama tetiklenir.
    private void OnThemeChanged() => _vm.RebuildTrendChart();
}
