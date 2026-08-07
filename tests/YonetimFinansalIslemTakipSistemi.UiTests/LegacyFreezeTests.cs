using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

namespace YonetimFinansalIslemTakipSistemi.UiTests;

/// <summary>
/// LEGACY FREEZE sözleşmesi (Faz E3).
///
/// Karar: eski pencereler bu sprintte SİLİNMEZ, DONDURULUR. Kabuk birkaç gün
/// gerçek kullanım görsün, kaldırma sonraki sprintte tek seferde yapılsın.
///
/// Donmanın bir bekçisi olmazsa "dondurduk" yalnızca bir niyet açıklaması
/// olur: yeni bir özellik farkında olmadan eski yola bağlanır ve kaldırma
/// sprinti her seferinde büyür. Buradaki testler donmayı DERLEME ZAMANINDA
/// ölçülebilir bir kurala çevirir.
///
/// Kaldırma listesi: docs/02-Architecture/Legacy-Shell-Migration.md
/// </summary>
public class LegacyFreezeTests
{
    /// <summary>
    /// Dondurulan sınıflar. Kaldırma sprintinde bu liste boşalacak.
    /// </summary>
    public static readonly string[] FrozenTypes =
    [
        "MainWindow",
        "AnalysisWindow",
        "AuditLogWindow",
        "CargoCompanyListWindow",
        "CargoDashboardWindow",
        "CargoOperationCenterWindow",
        "CargoShipmentListWindow",
        "CompanyDirectoryListWindow",
        "ExchangeRateWindow",
        "SystemHealthWindow",
        "MailContactListWindow",
        "UserPermissionWindow",
        "ReportWindow",
        "SystemLogsWindow",
        "UserManagementWindow",
        "WhatsAppContactListWindow",
    ];

    public static TheoryData<string> Frozen()
    {
        var data = new TheoryData<string>();
        foreach (var name in FrozenTypes) data.Add(name);
        return data;
    }

    private static readonly Assembly Ui = typeof(ScreenRegistry).Assembly;

    /// <summary>
    /// Tip DOĞRUDAN kullanılmaz, isimden çözülür: testin kendisi donmuş yola
    /// derleme bağı kurmamalı (kural: testler yalnızca kabuk üzerinden).
    /// </summary>
    private static Type Resolve(string typeName)
    {
        var type = Ui.GetTypes().SingleOrDefault(t => t.Name == typeName);
        Assert.True(type is not null, $"{typeName} bulunamadı.");
        return type!;
    }

    // ── Donma işareti ────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(Frozen))]
    public void Dondurulan_sinif_Obsolete_isaretli(string typeName)
    {
        var obsolete = Resolve(typeName).GetCustomAttribute<ObsoleteAttribute>();

        Assert.True(obsolete is not null,
            $"{typeName} [Obsolete] değil; donmuş yola yeni kod sessizce bağlanabilir.");

        // Gerekçe TEK kaynaktan gelmeli: kaldırma kararı değişirse tek yerde
        // değişsin, 16 dosyada değil.
        Assert.Equal(LegacyShellMigration.Reason, obsolete!.Message);
    }

    /// <summary>
    /// Gerekçe kullanıcıya değil GELİŞTİRİCİYE yazılmış bir yönergedir; ne
    /// yapılmayacağını ve ne zaman kaldırılacağını söylemeli.
    /// </summary>
    [Fact]
    public void Donma_gerekcesi_kaldirma_planini_gosteriyor()
    {
        Assert.Contains("Legacy - Shell Migration", LegacyShellMigration.Reason, StringComparison.Ordinal);
        Assert.Contains("Yeni kod eklemeyin",       LegacyShellMigration.Reason, StringComparison.Ordinal);
        Assert.Contains("Legacy Removal",           LegacyShellMigration.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Frozen))]
    public void Dondurulan_sinif_legacy_bolgesinde(string typeName)
    {
        var file = Directory
            .EnumerateFiles(UiSourceLocator.UiProjectDirectory, $"{typeName}.xaml.cs", SearchOption.AllDirectories)
            .SingleOrDefault();

        Assert.True(file is not null, $"{typeName}.xaml.cs bulunamadı.");

        var code = File.ReadAllText(file!, Encoding.UTF8);

        Assert.Contains("#region Legacy - Shell Migration", code, StringComparison.Ordinal);
        Assert.Contains("#endregion", code, StringComparison.Ordinal);
    }

    // ── Donmuş yola bağlar ───────────────────────────────────────────────

    /// <summary>
    /// BEKÇİ. Canlı koddan donmuş pencerelere kalan bağların TAMAMI burada
    /// sayılı. Yeni bir bağ eklenirse bu test düşer — "yeni kod bu sınıflara
    /// yazılmasın" kuralının otomatik karşılığı.
    ///
    /// Kalan altı bağ da "kabuk yoksa pencere aç" yedek dallarıdır ve
    /// pratikte ölüdür: kabukta Navigator her zaman atanır. Kaldırma
    /// sprintinde dallarla birlikte gidecekler.
    /// </summary>
    [Fact]
    public void Canli_koddan_donmus_pencerelere_bag_yalnizca_bilinen_yerlerde()
    {
        var beklenen = new Dictionary<string, int>
        {
            ["CargoDashboardScreen.xaml.cs"]   = 5,
            ["CargoShipmentListScreen.xaml.cs"] = 1,
        };

        var bulunan = new Dictionary<string, int>();

        foreach (var file in Directory.EnumerateFiles(
                     UiSourceLocator.UiProjectDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);

            // Donmuş dosyaların kendi içindeki bağlar donmanın parçası
            if (FrozenTypes.Any(t => name.Equals($"{t}.xaml.cs", StringComparison.Ordinal))) continue;
            if (name.Equals("LegacyShellMigration.cs", StringComparison.Ordinal)) continue;

            var code = File.ReadAllText(file, Encoding.UTF8);

            // Yorumlar sayılmamalı: donmayı ANLATAN yorumlar bu adları geçiriyor
            code = Regex.Replace(code, @"//[^\r\n]*", string.Empty);
            code = Regex.Replace(code, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

            var count = FrozenTypes.Sum(t => Regex.Matches(code, $@"\bnew\s+(\w+\.)*{t}\s*\(").Count);

            if (count > 0) bulunan[name] = count;
        }

        Assert.Equal(beklenen.OrderBy(k => k.Key), bulunan.OrderBy(k => k.Key));
    }

    /// <summary>
    /// Kabuk donmuş yolu HİÇ kullanmamalı. Kullansaydı kabuk kendi yerine
    /// geçtiği pencereyi açıyor olurdu.
    /// </summary>
    [Fact]
    public void Kabuk_donmus_pencere_acmiyor()
    {
        var code = File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
            "Views", "Shell", "ShellWindow.xaml.cs"), Encoding.UTF8);

        foreach (var type in FrozenTypes)
            Assert.DoesNotContain($"new {type}(", code, StringComparison.Ordinal);
    }

    /// <summary>
    /// Kayıt tablosu ekranları UserControl olarak üretir; bir satır pencereye
    /// işaret ederse kabuk sekme yerine pencere açardı.
    /// </summary>
    [Fact]
    public void Kayit_tablosu_donmus_pencereye_isaret_etmiyor()
    {
        var code = File.ReadAllText(Path.Combine(UiSourceLocator.UiProjectDirectory,
            "Common", "Shell", "ScreenRegistry.cs"), Encoding.UTF8);

        foreach (var type in FrozenTypes)
            Assert.DoesNotContain($"new {type}(", code, StringComparison.Ordinal);
    }

    /// <summary>
    /// Testler donmuş yolu değil kabuğu sınamalı: donmuş tipe derleme bağı
    /// kuran bir test, kaldırma sprintinde gereksiz engel olur.
    /// </summary>
    [Fact]
    public void Testler_donmus_tiplere_derleme_bagi_kurmuyor()
    {
        var ihlaller = new List<string>();

        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(UiSourceLocator.UiProjectDirectory, "..", "..", "tests"),
                     "*.cs", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            if (name.Equals(nameof(LegacyFreezeTests) + ".cs", StringComparison.Ordinal)) continue;
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;

            var code = File.ReadAllText(file, Encoding.UTF8);
            code = Regex.Replace(code, @"//[^\r\n]*", string.Empty);

            // Dize sabitleri sayılmaz: donmuş yolun YOKLUĞUNU doğrulayan
            // testler ("new MainWindow(" içermemeli) tam bu metinleri taşır.
            code = Regex.Replace(code, @"""(\\.|[^""\\])*""", "\"\"");

            foreach (var type in FrozenTypes)
                if (Regex.IsMatch(code, $@"\b(new\s+{type}\s*\(|typeof\s*\(\s*{type}\s*\))"))
                    ihlaller.Add($"{name} → {type}");
        }

        Assert.True(ihlaller.Count == 0,
            "Testler donmuş tipleri doğrudan kullanmamalı:\n  " + string.Join("\n  ", ihlaller));
    }

    // ── Kaldırma planı belgeli ───────────────────────────────────────────

    /// <summary>
    /// Donma ancak yazılı bir kaldırma planıyla anlamlı; plansız donma
    /// kalıcı teknik borçtur.
    /// </summary>
    [Fact]
    public void Kaldirma_plani_belgelenmis()
    {
        var doc = Path.Combine(UiSourceLocator.UiProjectDirectory,
            "..", "..", "docs", "02-Architecture", "Legacy-Shell-Migration.md");

        Assert.True(File.Exists(doc), "Kaldırma planı belgesi yok.");

        var text = File.ReadAllText(doc, Encoding.UTF8);

        // Envanter eksiksiz olmalı — belge listeyle birlikte kaymamalı
        foreach (var type in FrozenTypes)
            Assert.Contains(type, text, StringComparison.Ordinal);
    }
}
