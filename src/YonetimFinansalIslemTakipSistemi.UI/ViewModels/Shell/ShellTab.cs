using System.Windows;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Shell;

/// <summary>
/// Kabukta AÇIK olan bir sekme.
///
/// <see cref="ScreenDefinition"/> ekranın tarifidir (statik); bu sınıf o
/// tariften üretilmiş canlı örnektir. Ayrım önemli: aynı tanım birden çok kez
/// açılamaz, bu yüzden <see cref="Key"/> üzerinden tekillik aranır.
/// </summary>
public sealed class ShellTab
{
    public ShellTab(ScreenDefinition definition, FrameworkElement view)
    {
        Definition = definition;
        View       = view;
    }

    public ScreenDefinition Definition { get; }

    /// <summary>Sekmenin içeriği — ekranın UserControl'ü.</summary>
    public FrameworkElement View { get; }

    public ScreenKey Key      => Definition.Key;
    public string    Title    => Definition.Title;
    public bool      CanClose => Definition.CanClose;

    /// <summary>
    /// Ekran kapanmaya hazır mı. İçerik <see cref="IShellScreen"/> uyguluyorsa
    /// ona sorulur; uygulamıyorsa kaydedilmemiş durumu yok sayılır.
    /// </summary>
    public bool RequestClose() => View is not IShellScreen screen || screen.RequestClose();
}
