using System.Windows;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Analysis;

using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UI.Views.Analysis;

#region Legacy - Shell Migration

/// <summary>
/// Finans Analiz Merkezi — ince barındırıcı (Faz D6).
///
/// İçeriğin tamamı <see cref="AnalysisScreen"/>'de. Kurucu ViewModel alır
/// (mevcut sözleşme): analiz ekranı ViewModel'ini kendisi çözmüyor, dışarıdan
/// alıyor — çağıranlar değişmesin diye bu korundu.
/// </summary>
[Obsolete(LegacyShellMigration.Reason)]
public partial class AnalysisWindow : Window
{
    public AnalysisWindow(AnalysisViewModel viewModel)
    {
        InitializeComponent();
        ScreenHost.Content = new AnalysisScreen(viewModel);
    }
}

#endregion
