using System.Text;
using System.Text.RegularExpressions;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Ekranın kendi işine sahip olması — kabuk onu ikinci kez yapmamalı.
///
/// GEÇMİŞİ: bu sınıf Faz D'de "barındıran pencere ↔ ekran" sözleşmesiydi.
/// MainWindow ekranı barındırıyordu ve iki sessiz tuzak vardı: mantığın iki
/// yerde kalması ve ViewModel'in ikiye bölünmesi (Transient kayıt yüzünden
/// iki ayrı örnek → "F5 çalışmıyor" gibi görünen hata).
///
/// Pencere Faz F1'de silindi ama TUZAKLAR DURUYOR — yalnızca karşı taraf
/// değişti: artık ikinci sahip olabilecek olan KABUK. Sözleşme buraya
/// yönlendirildi; pencereye özgü olanlar (menü görünürlüğü, çıkış onayı)
/// karşılıksız kaldığı için düştü.
/// </summary>
public class ScreenOwnershipTests
{
    private static string ShellCode =>
        File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
            "Views", "Shell", "ShellWindow.xaml.cs"), Encoding.UTF8);

    private static string ShellMarkup =>
        Regex.Replace(
            File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
                "Views", "Shell", "ShellWindow.xaml"), Encoding.UTF8),
            @"<!--.*?-->", string.Empty, RegexOptions.Singleline);

    private static string ScreenCode =>
        File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
            "Views", "CashTransactions", "CashTransactionsScreen.xaml.cs"), Encoding.UTF8);

    /// <summary>
    /// Liste ViewModel'inin TEK sahibi ekrandır.
    ///
    /// CashTransactionListViewModel Transient kayıtlı: kabuk da çözerse İKİ
    /// AYRI örnek doğar, ekran bir listeyi gösterirken yenileme diğerini
    /// filtreler. Ekranda hiçbir şey değişmediği için hata "yenileme
    /// çalışmıyor" diye görünür.
    /// </summary>
    [Fact]
    public void Liste_viewmodelini_yalnizca_ekran_cozer()
    {
        Assert.Contains("GetRequiredService<CashTransactionListViewModel>", ScreenCode, StringComparison.Ordinal);
        Assert.DoesNotContain("CashTransactionListViewModel", ShellCode, StringComparison.Ordinal);
    }

    /// <summary>
    /// Kabuk hiçbir ekranın ViewModel'ini çözmemeli. Çözdüğü an o ekranın
    /// durumu iki yerden yönetilir hâle gelir.
    /// </summary>
    [Fact]
    public void Kabuk_ekran_viewmodeli_cozmuyor()
    {
        var resolved = Regex.Matches(ShellCode, @"GetRequiredService<(?<t>\w*ViewModel)>")
            .Select(m => m.Groups["t"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.True(resolved.Count == 0,
            "Kabuk ekran ViewModel'i çözüyor: " + string.Join(", ", resolved));
    }

    /// <summary>
    /// Her kısayol tuşunun karşılığında bir CommandBinding olmalı. Eksikse
    /// tuş hiçbir şey yapmaz — sessiz başarısızlık.
    ///
    /// Kabukta tuş atamaları pencere seviyesinde (odak rayda olsa da
    /// çalışsınlar diye), gövdeleri ekranda; bağlantıyı Command_Forward kurar.
    /// </summary>
    [Fact]
    public void Her_kisayol_tusunun_command_baglamasi_var()
    {
        var used = Regex.Matches(ShellMarkup, @"<KeyBinding[^>]*Command=""common:AppCommands\.(?<cmd>\w+)""")
            .Select(m => m.Groups["cmd"].Value)
            .ToHashSet(StringComparer.Ordinal);

        var bound = Regex.Matches(ShellMarkup, @"<CommandBinding\s+Command=""common:AppCommands\.(?<cmd>\w+)""")
            .Select(m => m.Groups["cmd"].Value)
            .ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(used);

        var orphan = used.Where(c => !bound.Contains(c)).OrderBy(c => c, StringComparer.Ordinal).ToList();
        Assert.True(orphan.Count == 0,
            "KeyBinding var ama CommandBinding yok: " + string.Join(", ", orphan));
    }

    /// <summary>
    /// Beklenen kısayol kümesi. Pencereden kabuğa geçişte hiçbiri düşmemeli.
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
        Assert.Contains($@"Command=""common:AppCommands.{command}""", ShellMarkup, StringComparison.Ordinal);
    }

    /// <summary>
    /// İşlem mantığı yalnızca ekranda olmalı; kabukta kopyası bulunması
    /// taşıma yerine kopyalama yapıldığı anlamına gelir.
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
    public void Ekran_isi_kabukta_kopyalanmadi(string member)
    {
        Assert.Contains(member, ScreenCode, StringComparison.Ordinal);
        Assert.DoesNotContain(member, ShellCode, StringComparison.Ordinal);
    }

    /// <summary>
    /// Ekran oturumu kendisi kapatmaz: çıkış kabuğun işidir (onay + audit +
    /// pencere kapatma tek yerde). Ekranların kendi çıkış düğmeleri Faz F1'de
    /// kaldırıldı; kalıntı bir Shutdown/LoginWindow çağrısı da olmamalı.
    /// </summary>
    [Fact]
    public void Ekran_oturumu_kendisi_kapatmiyor()
    {
        Assert.DoesNotContain("Application.Current.Shutdown", ScreenCode, StringComparison.Ordinal);
        Assert.DoesNotContain("LoginWindow", ScreenCode, StringComparison.Ordinal);
        Assert.DoesNotContain("LogoutRequested", ScreenCode, StringComparison.Ordinal);
    }

    /// <summary>
    /// Ekranın açtığı alt pencereler sahibini AĞAÇTAN bulmalı
    /// (<c>Window.GetWindow(this)</c>). Sabit bir pencereye bağlanan ekran
    /// kabukta sahipsiz diyalog açar.
    /// </summary>
    [Fact]
    public void Alt_pencereler_sahibini_agactan_bulur()
    {
        Assert.Contains("Window.GetWindow(this)", ScreenCode, StringComparison.Ordinal);

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
