using System.Text;
using System.Text.RegularExpressions;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Nakit İşlemler içeriği UserControl'e taşındıktan sonra barındıran pencere ile
/// ekran arasındaki sözleşmeyi korur (Faz D pilotu).
///
/// NEDEN VAR: İçeriği ayırmanın iki sessiz tuzağı var ve ikisi de derlenir:
///
///   1) MANTIK İKİ YERDE KALIR. Pencere eski handler'ının kopyasını tutar,
///      ekran da kendisininkini. Bir tarafı düzeltirsiniz, diğeri eski
///      davranışta kalır.
///
///   2) VIEWMODEL İKİYE BÖLÜNÜR. CashTransactionListViewModel Transient kayıtlı
///      (App.xaml.cs). Hem pencere hem ekran DI'dan çözerse İKİ AYRI örnek
///      oluşur; ekran bir listeyi gösterirken F5 diğerini filtreler. Ekranda
///      hiçbir şey değişmediği için hata "F5 çalışmıyor" diye görünür.
///
/// Bu testler kaynak metni okur; ikisi de çalışma zamanında değil, kod
/// yazılırken yakalanmalı.
/// </summary>
public class ScreenHostContractTests
{
    private static string MainWindowCode =>
        File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory, "MainWindow.xaml.cs"), Encoding.UTF8);

    private static string MainWindowXaml =>
        File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory, "MainWindow.xaml"), Encoding.UTF8);

    /// <summary>
    /// Yorumsuz XAML. Buradaki yorumlar taşımanın NEDENİNİ anlatırken eski
    /// işaretlemeden örnek veriyor; tarama onları gerçek kod sanmamalı.
    /// </summary>
    private static string MainWindowMarkup =>
        Regex.Replace(MainWindowXaml, @"<!--.*?-->", string.Empty, RegexOptions.Singleline);

    private static string ScreenCode =>
        File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
            "Views", "CashTransactions", "CashTransactionsScreen.xaml.cs"), Encoding.UTF8);

    /// <summary>
    /// Liste ViewModel'inin tek sahibi ekrandır. Pencere ayrıca çözerse
    /// Transient kayıt yüzünden ikinci bir örnek doğar.
    /// </summary>
    [Fact]
    public void Liste_viewmodelini_yalnizca_ekran_cozer()
    {
        Assert.DoesNotContain("CashTransactionListViewModel", MainWindowCode);
        Assert.Contains("GetRequiredService<CashTransactionListViewModel>", ScreenCode);
    }

    /// <summary>
    /// Pencerenin DataContext'i yok; XAML'de veri bağlama da olmamalı. Aksi
    /// hâlde binding sessizce boşa düşer (WPF binding hatası uygulamayı
    /// durdurmaz, sadece kısayol çalışmaz).
    /// </summary>
    [Fact]
    public void Pencere_xamlinde_veri_baglama_yok()
    {
        var bindings = Regex.Matches(MainWindowMarkup, @"\{Binding[^}]*\}")
                            .Select(m => m.Value)
                            .ToList();

        Assert.True(bindings.Count == 0,
            "MainWindow.xaml artık DataContext taşımıyor; kalan binding: " + string.Join(", ", bindings));
    }

    /// <summary>
    /// Her kısayol tuşunun karşılığında bir CommandBinding olmalı. Eksikse tuş
    /// hiçbir şey yapmaz — sessiz başarısızlık.
    /// </summary>
    [Fact]
    public void Her_kisayol_tusunun_command_baglamasi_var()
    {
        var xaml = MainWindowMarkup;

        var used = Regex.Matches(xaml, @"<KeyBinding[^>]*Command=""common:AppCommands\.(?<cmd>\w+)""")
                        .Select(m => m.Groups["cmd"].Value)
                        .ToHashSet(StringComparer.Ordinal);

        var bound = Regex.Matches(xaml, @"<CommandBinding\s+Command=""common:AppCommands\.(?<cmd>\w+)""")
                         .Select(m => m.Groups["cmd"].Value)
                         .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(used);

        var orphan = used.Where(c => !bound.Contains(c)).OrderBy(c => c).ToList();
        Assert.True(orphan.Count == 0,
            "KeyBinding var ama CommandBinding yok: " + string.Join(", ", orphan));
    }

    /// <summary>
    /// Beklenen kısayol kümesi. Faz D taşımasında hiçbiri düşmemeli.
    /// </summary>
    [Theory]
    [InlineData("New")]
    [InlineData("Duplicate")]
    [InlineData("DeleteSelected")]
    [InlineData("FocusSearch")]
    [InlineData("ImportExcel")]
    [InlineData("RefreshList")]
    public void Kisayol_korundu(string command)
    {
        Assert.Contains($@"Command=""common:AppCommands.{command}""", MainWindowMarkup);
    }

    /// <summary>
    /// Kısayol gövdeleri ekrana YÖNLENDİRİLİR; pencerede iş mantığı çalıştırmaz.
    /// Her Command_* handler'ı tek satırlık bir <c>_screen.X()</c> ifadesi olmalı.
    /// </summary>
    [Fact]
    public void Kisayol_handlerlari_ekrana_yonlendirir()
    {
        var handlers = Regex.Matches(MainWindowCode,
            @"private\s+void\s+(?<name>Command_\w+)\s*\([^)]*\)\s*=>\s*(?<body>[^;]+);");

        Assert.True(handlers.Count >= 6,
            $"Beklenen 6 kısayol handler'ı, bulunan {handlers.Count}. Gövdeli (blok) handler iş mantığı taşıyor olabilir.");

        var wrong = handlers.Select(m => (Name: m.Groups["name"].Value, Body: m.Groups["body"].Value.Trim()))
                            .Where(h => !h.Body.StartsWith("_screen.", StringComparison.Ordinal))
                            .ToList();

        Assert.True(wrong.Count == 0,
            "Ekrana yönlendirmeyen handler: " + string.Join(", ", wrong.Select(h => $"{h.Name} => {h.Body}")));
    }

    /// <summary>
    /// Taşınan işlem handler'ları yalnızca ekranda olmalı. İkisinde birden
    /// bulunmaları, taşıma yerine kopyalama yapıldığı anlamına gelir.
    /// </summary>
    [Theory]
    [InlineData("NewTransactionButton_Click")]
    [InlineData("EditTransactionButton_Click")]
    [InlineData("CopyTransactionButton_Click")]
    [InlineData("DeleteTransactionButton_Click")]
    [InlineData("CashImportButton_Click")]
    [InlineData("BalanceCard_Click")]
    [InlineData("ApplySavedLayoutAsync")]
    [InlineData("SaveGridLayoutAsync")]
    [InlineData("ResetGridLayoutAsync")]
    [InlineData("ApplyColumnHeaderContextMenu")]
    [InlineData("ApplyBalanceColumnVisibility")]
    public void Tasinan_uye_pencerede_kopyalanmadi(string member)
    {
        Assert.Contains(member, ScreenCode);
        Assert.DoesNotContain(member, MainWindowCode);
    }

    /// <summary>
    /// Menü ve pencere-seviyesi sorumluluklar EKRANA sızmamalı; ters yönde
    /// kopyalama da aynı bakım sorununu doğurur.
    /// </summary>
    [Theory]
    [InlineData("RefreshMenuVisibility")]
    [InlineData("StartupUpdateChecker")]
    [InlineData("IsLogoutRequested")]
    public void Pencere_seviyesi_uye_ekrana_sizmadi(string member)
    {
        Assert.Contains(member, MainWindowCode);
        Assert.DoesNotContain(member, ScreenCode);
    }

    /// <summary>
    /// Çıkış sözleşmesi: ekran yalnızca HABER VERİR, pencereyi kapatmaz.
    /// Onay + audit + IsLogoutRequested penceredeki tek noktada kalır —
    /// aynı ekran ileride kabuk içinde de barındığında sözleşme değişmesin.
    /// </summary>
    [Fact]
    public void Ekran_cikisi_yalnizca_olayla_bildirir()
    {
        Assert.Contains("event Action? LogoutRequested", ScreenCode);
        Assert.Contains("LogoutRequested += ", MainWindowCode);

        Assert.DoesNotContain("Application.Current.Shutdown", ScreenCode);
        Assert.DoesNotContain("LoginWindow", ScreenCode);
    }

    /// <summary>
    /// Ekranın açtığı alt pencereler sahibini AĞAÇTAN bulmalı
    /// (<c>Window.GetWindow(this)</c>). MainWindow'a sabitlenirse aynı ekran
    /// kabuk içinde barındığında sahipsiz diyalog açar.
    /// </summary>
    [Fact]
    public void Alt_pencereler_sahibini_agactan_bulur()
    {
        Assert.Contains("Window.GetWindow(this)", ScreenCode);
        Assert.DoesNotContain("Owner = (MainWindow)", ScreenCode);

        var ownerAssignments = Regex.Matches(ScreenCode, @"Owner\s*=\s*(?<value>[^,;\s}]+)")
                                    .Select(m => m.Groups["value"].Value)
                                    .Distinct(StringComparer.Ordinal)
                                    .ToList();

        Assert.NotEmpty(ownerAssignments);

        var hardcoded = ownerAssignments.Where(v => !v.Contains("HostWindow", StringComparison.Ordinal)).ToList();
        Assert.True(hardcoded.Count == 0,
            "Owner ağaçtan alınmıyor: " + string.Join(", ", hardcoded));
    }
}
