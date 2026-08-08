using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;
using YonetimFinansalIslemTakipSistemi.UI.ViewModels.Shell;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Kabuk erişilebilirliği (Faz F7).
///
/// Faz E'nin gerçek uygulama doğrulamasında ortaya çıktı: çalışan kabuğun
/// otomasyon ağacı okunduğunda sekmenin adı başlık değil HAM TİP ADIYDI
/// ("…ViewModels.Shell.ShellTab"). Ekran okuyucu kullanan biri hangi sekmede
/// olduğunu duyamazdı; otomasyon araçları da sekmeleri ayırt edemezdi.
///
/// Simge içerikli düğmeler ("✕") de tek başına anlam taşımaz.
/// </summary>
public class ShellAccessibilityTests
{
    private static string ShellMarkup =>
        Regex.Replace(
            File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
                "Views", "Shell", "ShellWindow.xaml"), Encoding.UTF8),
            @"<!--.*?-->", string.Empty, RegexOptions.Singleline);

    /// <summary>
    /// Sekme başlığı otomasyona AD olarak ulaşmalı. TabItem'ın otomasyon adı
    /// başlık nesnesinin metin karşılığından türer; ShellTab bunu vermezse ham
    /// tip adı okunur.
    /// </summary>
    [Fact]
    public void Sekme_otomasyon_adi_baslik() => ThemeTestHost.Run(() =>
    {
        var definition = new ScreenDefinition(
            ScreenKey.Reports, "Raporlar", [],
            CreateView: _ => new UserControl());

        var tab = new ShellTab(definition, new UserControl());

        Assert.Equal("Raporlar", tab.ToString());
        Assert.DoesNotContain("ShellTab", tab.ToString(), StringComparison.Ordinal);
    });

    /// <summary>
    /// Parametreli ekranlarda başlık kayda özgüdür; otomasyon adı da onu
    /// göstermeli — iki operasyon sekmesi birbirinden ayırt edilebilmeli.
    /// </summary>
    [Fact]
    public void Parametreli_sekmenin_otomasyon_adi_kayda_ozgu() => ThemeTestHost.Run(() =>
    {
        var definition = new ScreenDefinition(
            ScreenKey.CargoOperationCenter, "Operasyon Merkezi", [],
            CreateInstance: (_, _) => new ScreenInstance("1", "Operasyon — GDN-2026-0042", new UserControl()),
            IsParameterized: true);

        var tab = new ShellTab(definition, new UserControl(), "1", "Operasyon — GDN-2026-0042");

        Assert.Equal("Operasyon — GDN-2026-0042", tab.ToString());
    });

    /// <summary>
    /// Otomasyon eşi gerçekten bu adı görüyor mu — ToString'in okunduğunu
    /// varsaymak yetmez, TabItem üzerinden ölçülür.
    /// </summary>
    [Fact]
    public void TabItem_otomasyon_esi_basligi_okuyor() => ThemeTestHost.Run(() =>
    {
        var definition = new ScreenDefinition(
            ScreenKey.Analysis, "Finans Analiz", [],
            CreateView: _ => new UserControl());

        // Gerçek kap üzerinden ölçülür: otomasyon eşi TabControl bağlamında kurulur
        var tabs = new TabControl
        {
            ItemsSource = new[] { new ShellTab(definition, new UserControl()) }
        };

        var window = new System.Windows.Window { Content = tabs, Width = 300, Height = 200 };
        window.Show();
        window.UpdateLayout();

        var container = (TabItem)tabs.ItemContainerGenerator.ContainerFromIndex(0);
        var peer      = UIElementAutomationPeer.CreatePeerForElement(container);

        var name = peer.GetName();
        window.Close();

        Assert.Equal("Finans Analiz", name);
    });

    /// <summary>
    /// Simge düğmeleri adlandırılmalı: "✕" tek başına ne olduğunu söylemez.
    /// </summary>
    [Fact]
    public void Sekme_kapatma_dugmesi_adlandirilmis()
    {
        var button = Regex.Match(ShellMarkup, @"<Button Content=""✕""[\s\S]*?Click=""CloseTabButton_Click""").Value;

        Assert.NotEmpty(button);
        Assert.Contains(@"AutomationProperties.Name=""Sekmeyi kapat""", button, StringComparison.Ordinal);
        Assert.Contains("ToolTip=", button, StringComparison.Ordinal);
    }

    /// <summary>
    /// Komut paletinin arama kutusunun görünür bir etiketi yok; ekran okuyucu
    /// için ad ve yardım metni açıkça verilmeli.
    /// </summary>
    [Fact]
    public void Komut_paleti_adlandirilmis()
    {
        var query = Regex.Match(ShellMarkup, @"<TextBox x:Name=""PaletteQuery""[\s\S]*?/>").Value;

        Assert.NotEmpty(query);
        Assert.Contains("AutomationProperties.Name", query, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.HelpText", query, StringComparison.Ordinal);

        var results = Regex.Match(ShellMarkup, @"<ListBox x:Name=""PaletteResults""[\s\S]*?>").Value;
        Assert.Contains("AutomationProperties.Name", results, StringComparison.Ordinal);
    }

    /// <summary>
    /// Kabuğun ana bölgeleri adlandırılmalı: kullanıcı hangi bölgede olduğunu
    /// duyabilmeli.
    /// </summary>
    [Theory]
    [InlineData("ScreenTabs")]
    [InlineData("NavigationGroupList")]
    public void Kabuk_bolgeleri_adlandirilmis(string elementName)
    {
        var element = Regex.Match(ShellMarkup, $@"x:Name=""{elementName}""[\s\S]*?>").Value;

        Assert.NotEmpty(element);
        Assert.Contains("AutomationProperties.Name", element, StringComparison.Ordinal);
    }

    /// <summary>
    /// ODAK SIRASI: palet açıldığında odak arama kutusuna gider ve yazarken
    /// orada kalır — gezinme ↑↓ ile ViewModel üzerinden yapılır. Sonuç listesi
    /// odak almazsa kullanıcı yazmaya devam edebilir.
    /// </summary>
    [Fact]
    public void Palet_odagi_arama_kutusunda_kaliyor()
    {
        var results = Regex.Match(ShellMarkup, @"<ListBox x:Name=""PaletteResults""[\s\S]*?>").Value;
        Assert.Contains(@"Focusable=""False""", results, StringComparison.Ordinal);

        var code = File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
            "Views", "Shell", "ShellWindow.xaml.cs"), Encoding.UTF8);

        Assert.Contains("PaletteQuery.Focus()", code, StringComparison.Ordinal);
    }
}
