using System.Windows;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UI.ViewModels.Shell;

/// <summary>
/// Kabukta AÇIK olan bir sekme.
///
/// <see cref="ScreenDefinition"/> ekranın tarifidir (statik); bu sınıf o
/// tariften üretilmiş canlı örnektir.
///
/// KİMLİK: Tekil ekranlarda <see cref="Key"/> yeterlidir. Parametreli
/// ekranlarda aynı tür altında birden çok sekme olabileceği için kimlik
/// <see cref="Key"/> + <see cref="InstanceKey"/> çiftidir — iki farklı kargo
/// ayrı sekmelerde açılır, aynı kargo ikinci kez açılmaz.
/// </summary>
public sealed class ShellTab
{
    public ShellTab(ScreenDefinition definition, FrameworkElement view, string? instanceKey = null, string? title = null)
    {
        Definition  = definition;
        View        = view;
        InstanceKey = instanceKey;
        Title       = title ?? definition.Title;
    }

    public ScreenDefinition Definition { get; }

    /// <summary>Sekmenin içeriği — ekranın UserControl'ü.</summary>
    public FrameworkElement View { get; }

    /// <summary>
    /// Parametreli ekranlarda örneği ayırt eden anahtar; tekil ekranlarda null.
    /// </summary>
    public string? InstanceKey { get; }

    /// <summary>
    /// Sekme başlığı. Parametreli ekranlarda kayda özgü olabilir
    /// (ör. "Operasyon — KRG-2026-0042").
    /// </summary>
    public string Title { get; }

    public ScreenKey Key      => Definition.Key;
    public bool      CanClose => Definition.CanClose;

    /// <summary>Bu sekme verilen kimliğe mi ait.</summary>
    public bool Matches(ScreenKey key, string? instanceKey)
        => Key == key && string.Equals(InstanceKey, instanceKey, StringComparison.Ordinal);

    /// <summary>
    /// Ekran kapanmaya hazır mı. İçerik <see cref="IShellScreen"/> uyguluyorsa
    /// ona sorulur; uygulamıyorsa kaydedilmemiş durumu yok sayılır.
    /// </summary>
    public bool RequestClose() => View is not IShellScreen screen || screen.RequestClose();
}
