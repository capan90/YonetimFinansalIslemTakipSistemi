using System.Windows;
using System.Windows.Controls;
using YonetimFinansalIslemTakipSistemi.UI.Common;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Analysis;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Analysis;

public partial class AnalysisScreen : UserControl
{
    private readonly AnalysisViewModel _vm;

    public AnalysisScreen(AnalysisViewModel vm)
    {
        InitializeComponent();
        _vm         = vm;
        DataContext = vm;

        // Grafik SkiaSharp ile çizilir ve DynamicResource'u görmez: tema
        // değişince seriler elle yeniden kurulmalı, yoksa eski renkte donar.
        // Abonelik Loaded/Unloaded ile SİMETRİK kurulur (bkz. BindThemeRepaint) —
        // kurucuda abone olan eski desen, ilk sekme geçişinden sonra aboneliğini
        // kaybediyordu.
        ScreenData.BindThemeRepaint(this, OnThemeChanged);

        ScreenData.Bind(this, () => _vm.LoadAsync());
    }

    // Boş durum görünürlüğü XAML'de DataTrigger ile bağlıdır (HasTrendData);
    // burada yalnızca yeniden boyama tetiklenir.
    private void OnThemeChanged() => _vm.RebuildTrendChart();
}
