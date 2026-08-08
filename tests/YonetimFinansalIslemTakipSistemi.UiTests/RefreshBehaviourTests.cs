using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// Yenileme davranışı (Faz F5).
///
/// Faz E1 otomatik tazelemeyi kaldırdı: sekme geçişi artık sorgu tetiklemiyor.
/// Geriye iki soru kaldı ve ikisi de kullanıcının işine dokunuyor:
///
///   1) Veriyi DEĞİŞTİREN bir adımdan sonra liste kendiliğinden tazelenmeli —
///      ama YALNIZCA gerçekten değişiklik olduysa.
///   2) Tazeleme kullanıcının yerini kaybettirmemeli: seçili satır ve
///      kaydırma konumu korunmalı.
/// </summary>
public class RefreshBehaviourTests
{
    private sealed record Row(int Id, string Name);

    private static DataGrid GridWith(params Row[] rows)
    {
        var grid = new DataGrid { ItemsSource = rows.ToList() };

        var window = new Window { Content = grid, Width = 400, Height = 300 };
        window.Show();
        window.UpdateLayout();

        return grid;
    }

    // ── Seçim korunması ──────────────────────────────────────────────────

    [Fact]
    public void Yenileme_secili_satiri_koruyor() => ThemeTestHost.Run(async () =>
    {
        var grid = GridWith(new Row(1, "bir"), new Row(2, "iki"), new Row(3, "üç"));
        grid.SelectedItem = grid.Items[1];

        await ScreenData.RefreshPreservingSelectionAsync(
            grid,
            item => (item as Row)?.Id,
            () =>
            {
                // Yenileme koleksiyonu BAŞTAN kurar — yeni örnekler, aynı kimlikler
                grid.ItemsSource = new List<Row> { new(1, "bir"), new(2, "iki"), new(3, "üç") };
                grid.UpdateLayout();
                return Task.CompletedTask;
            });

        Assert.Equal(2, ((Row)grid.SelectedItem).Id);
    });

    /// <summary>
    /// Seçim SIRAYLA değil ANAHTARLA geri kurulmalı: yenilemede satır sırası
    /// değişmiş olabilir (yeni kayıt, silinen kayıt, değişen filtre). Sıra ile
    /// geri kurmak yanlış satırı seçer ve kullanıcı bunu fark etmez.
    /// </summary>
    [Fact]
    public void Sira_degisse_de_ayni_kayit_secili_kaliyor() => ThemeTestHost.Run(async () =>
    {
        var grid = GridWith(new Row(1, "bir"), new Row(2, "iki"), new Row(3, "üç"));
        grid.SelectedItem = grid.Items[2];   // Id = 3

        await ScreenData.RefreshPreservingSelectionAsync(
            grid,
            item => (item as Row)?.Id,
            () =>
            {
                // Sıra tersine döndü + araya yeni kayıt girdi
                grid.ItemsSource = new List<Row> { new(3, "üç"), new(9, "dokuz"), new(2, "iki"), new(1, "bir") };
                grid.UpdateLayout();
                return Task.CompletedTask;
            });

        Assert.Equal(3, ((Row)grid.SelectedItem).Id);
    });

    /// <summary>
    /// Kayıt listede kalmadıysa (silindi ya da filtre dışına çıktı) seçim
    /// ZORLANMAZ. Uydurma bir satır seçmek, kullanıcının başka bir kaydı
    /// seçili sanmasına yol açardı — sonraki "Sil" tıklaması yanlış kaydı
    /// hedeflerdi.
    /// </summary>
    [Fact]
    public void Silinen_kayit_icin_baska_satir_secilmiyor() => ThemeTestHost.Run(async () =>
    {
        var grid = GridWith(new Row(1, "bir"), new Row(2, "iki"));
        grid.SelectedItem = grid.Items[1];   // Id = 2

        await ScreenData.RefreshPreservingSelectionAsync(
            grid,
            item => (item as Row)?.Id,
            () =>
            {
                grid.ItemsSource = new List<Row> { new(1, "bir") };   // 2 silindi
                grid.UpdateLayout();
                return Task.CompletedTask;
            });

        Assert.Null(grid.SelectedItem);
    });

    [Fact]
    public void Secim_yokken_yenileme_secim_uretmiyor() => ThemeTestHost.Run(async () =>
    {
        var grid = GridWith(new Row(1, "bir"), new Row(2, "iki"));
        Assert.Null(grid.SelectedItem);

        await ScreenData.RefreshPreservingSelectionAsync(
            grid,
            item => (item as Row)?.Id,
            () => Task.CompletedTask);

        Assert.Null(grid.SelectedItem);
    });

    // ── Kaynak sözleşmesi ────────────────────────────────────────────────

    private static string Source(params string[] path) =>
        File.ReadAllText(Path.Combine([UiSourceLocator.UiProjectDirectory, .. path]), Encoding.UTF8);

    /// <summary>
    /// SEKMEYE DÖNMEK TEK BAŞINA SORGU SEBEBİ DEĞİL.
    ///
    /// Kargo listesi tek istisnaydı: operasyon merkezi ayrı sekmede kaydı
    /// değiştirebildiği için her dönüşte yeniden sorguluyordu. Artık soru
    /// açık soruluyor — merkez gerçekten değiştirdi mi?
    /// </summary>
    [Fact]
    public void Sekmeye_donus_yalnizca_gercek_degisiklikte_sorgu_atiyor()
    {
        var code = Source("Views", "Cargo", "CargoShipmentListScreen.xaml.cs");

        var handler = Regex.Match(code, @"IsVisibleChanged \+=[\s\S]*?\n        \};").Value;

        Assert.NotEmpty(handler);
        Assert.Contains("ConsumeOperationCenterChange()", handler, StringComparison.Ordinal);
    }

    /// <summary>
    /// Değişiklik bayrağı TÜKETİLMELİ: aynı değişiklik için ikinci kez sorgu
    /// atılmamalı.
    /// </summary>
    [Fact]
    public void Degisiklik_bayragi_tuketiliyor()
    {
        var list = Source("Views", "Cargo", "CargoShipmentListScreen.xaml.cs");
        Assert.Contains("ClearModified()", list, StringComparison.Ordinal);

        var opCenter = Source("Views", "Cargo", "CargoOperationCenterScreen.xaml.cs");
        Assert.Contains("public void ClearModified()", opCenter, StringComparison.Ordinal);
    }

    /// <summary>
    /// Açılan ekrana ZAYIF referans: operasyon merkezi sekmesi kapandığında
    /// liste onu bellekte tutmamalı (bkz. Faz F4 ölçümü).
    /// </summary>
    [Fact]
    public void Acilan_ekrana_zayif_referans_tutuluyor()
    {
        var code = Source("Views", "Cargo", "CargoShipmentListScreen.xaml.cs");

        Assert.Contains("WeakReference<CargoOperationCenterScreen>", code, StringComparison.Ordinal);
    }

    /// <summary>
    /// Veri değiştiren modal adımlardan sonra tazeleme KOŞULLU olmalı:
    /// kullanıcı iptal ettiyse sorgu atılmamalı.
    /// </summary>
    [Theory]
    [InlineData("Views", "Cargo", "CargoShipmentListScreen.xaml.cs")]
    [InlineData("Views", "CashTransactions", "CashTransactionsScreen.xaml.cs")]
    [InlineData("Views", "Cargo", "CompanyDirectoryListScreen.xaml.cs")]
    [InlineData("Views", "Cargo", "CargoCompanyListScreen.xaml.cs")]
    [InlineData("Views", "WhatsApp", "WhatsAppContactListScreen.xaml.cs")]
    [InlineData("Views", "Mail", "MailContactListScreen.xaml.cs")]
    [InlineData("Views", "Users", "UserManagementScreen.xaml.cs")]
    public void Modal_sonrasi_tazeleme_kosullu(params string[] path)
    {
        var code = Source(path);
        code = Regex.Replace(code, @"//[^\r\n]*", string.Empty);

        // "ShowDialog();" ardından KOŞULSUZ yenileme olmamalı
        var unconditional = Regex.Matches(code,
            @"\.ShowDialog\(\);\s*(await\s+)?(_vm|_listVm|_viewModel)\.\w*Load\w*Async\(\)");

        Assert.True(unconditional.Count == 0,
            $"{path[^1]}: modal iptal edilse bile sorgu atılıyor ({unconditional.Count} yer).");
    }

    /// <summary>
    /// Seçim korunması iki ana listede kullanılıyor olmalı — en çok kayıt
    /// gezilen ekranlar bunlar.
    /// </summary>
    [Theory]
    [InlineData("Views", "Cargo", "CargoShipmentListScreen.xaml.cs")]
    [InlineData("Views", "CashTransactions", "CashTransactionsScreen.xaml.cs")]
    public void Ana_listeler_secimi_koruyarak_tazeliyor(params string[] path)
    {
        Assert.Contains("RefreshPreservingSelectionAsync", Source(path), StringComparison.Ordinal);
    }
}
