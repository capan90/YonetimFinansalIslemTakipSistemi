using System.Windows;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Analysis;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Analysis;

/// <summary>
/// Finans Analiz Merkezi — ince barındırıcı (Faz D6).
///
/// İçeriğin tamamı <see cref="AnalysisScreen"/>'de. Kurucu ViewModel alır
/// (mevcut sözleşme): analiz ekranı ViewModel'ini kendisi çözmüyor, dışarıdan
/// alıyor — çağıranlar değişmesin diye bu korundu.
/// </summary>
public partial class AnalysisWindow : Window
{
    public AnalysisWindow(AnalysisViewModel viewModel)
    {
        InitializeComponent();
        ScreenHost.Content = new AnalysisScreen(viewModel);
    }
}
