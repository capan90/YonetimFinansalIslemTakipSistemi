using System.Windows;
using System.Windows.Controls;

namespace YonetimFinansalIslemTakipSistemi.UI.Common;

/// <summary>
/// Kırpılan metin için otomatik ToolTip.
///
/// WPF'te WinUI'daki <c>IsTextTrimmed</c> karşılığı yoktur; kırpılma ancak
/// ölçülerek anlaşılır. Her hücreye koşulsuz ToolTip koymak da iyi değil —
/// zaten okunabilen kısa değerlerde gereksiz balon çıkar.
///
/// Bu ekli özellik metni sonsuz genişlikte yeniden ölçer; doğal genişlik
/// ayrılan yerden büyükse ToolTip'i tam değerle kurar, değilse kaldırır.
/// </summary>
public static class TextTrimmingToolTip
{
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached(
            "Enabled", typeof(bool), typeof(TextTrimmingToolTip),
            new PropertyMetadata(false, OnEnabledChanged));

    public static void SetEnabled(DependencyObject element, bool value)
        => element.SetValue(EnabledProperty, value);

    public static bool GetEnabled(DependencyObject element)
        => (bool)element.GetValue(EnabledProperty);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock) return;

        if ((bool)e.NewValue)
        {
            textBlock.SizeChanged        += OnSizeChanged;
            // DataGrid satırları geri dönüştürülürken hücre genişliği değişmez,
            // yalnızca DataContext değişir — SizeChanged tetiklenmediği için
            // ToolTip eski satırın değerinde kalırdı.
            textBlock.DataContextChanged += OnDataContextChanged;
        }
        else
        {
            textBlock.SizeChanged        -= OnSizeChanged;
            textBlock.DataContextChanged -= OnDataContextChanged;
        }
    }

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
        => Update((TextBlock)sender);

    private static void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var textBlock = (TextBlock)sender;
        // Binding henüz uygulanmamış olabilir; yerleşim tamamlandıktan sonra ölç
        textBlock.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Loaded,
            new Action(() => Update(textBlock)));
    }

    private static void Update(TextBlock textBlock)
    {
        if (string.IsNullOrEmpty(textBlock.Text))
        {
            ToolTipService.SetToolTip(textBlock, null);
            return;
        }

        // Sonsuz genişlikte yeniden ölçüm doğal metin genişliğini verir.
        // Arrange sonrası çağrıldığı için mevcut yerleşimi bozmaz.
        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        // 0.5 px tolerans: yuvarlama farkı yüzünden kırpılmamış metne balon çıkmasın
        var isTrimmed = textBlock.DesiredSize.Width > textBlock.ActualWidth + 0.5;

        ToolTipService.SetToolTip(textBlock, isTrimmed ? textBlock.Text : null);
    }
}
